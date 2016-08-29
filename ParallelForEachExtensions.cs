using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    /// <summary>
    /// Extensions methods for IEnumerable and IAsyncEnumerable to do parallel for-each loop in async-await manner
    /// </summary>
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    public static class ParallelForEachExtensions
    {
        private class ParallelForEachContext
        {
            private SemaphoreSlim _semaphore;
            private TaskCompletionSource<object> _completionTcs;
            private List<Exception> _exceptionList;
            private SpinLock _exceptionListLock;
            private int _maxDegreeOfParalellism;
            private bool _breakLoopOnException;

            public ParallelForEachContext(int maxDegreeOfParalellism, bool breakLoopOnException)
            {
                if (maxDegreeOfParalellism < 0)
                    throw new ArgumentException($"The maximum degree of paralellism must be a non-negative number, but got {maxDegreeOfParalellism}", nameof(maxDegreeOfParalellism));
                if (maxDegreeOfParalellism == 0)
                    maxDegreeOfParalellism = Environment.ProcessorCount - 1;
                if (maxDegreeOfParalellism <= 0)
                    maxDegreeOfParalellism = 1;

                _semaphore = new SemaphoreSlim(initialCount: maxDegreeOfParalellism, maxCount: maxDegreeOfParalellism + 1);
                _completionTcs = new TaskCompletionSource<object>();
                _exceptionListLock = new SpinLock(enableThreadOwnerTracking: false);
                _maxDegreeOfParalellism = maxDegreeOfParalellism;
                _breakLoopOnException = breakLoopOnException;
            }

            public Task CompletionTask => _completionTcs.Task;

            public bool IsLoopBreakRequested;

            public void AddException(Exception ex)
            {
                bool lockTaken = false;
                while (!lockTaken)
                    _exceptionListLock.Enter(ref lockTaken);
                try
                {
                    if (_exceptionList == null)
                        _exceptionList = new List<Exception>();
                    _exceptionList.Add(ex);

                    if (_breakLoopOnException)
                        IsLoopBreakRequested = true;
                }
                finally
                {
                    _exceptionListLock.Exit(useMemoryBarrier: false);
                }
            }

            public Task OnStartOperationAsync(CancellationToken cancellationToken)
            {
                return _semaphore.WaitAsync(cancellationToken);
            }

            public void OnOperationComplete(Exception exceptionIfFailed = null)
            {
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // The For-Each loop has been canceled,
                    // and we don't care about the result any more
                    return;
                }

                if (exceptionIfFailed != null)
                    AddException(exceptionIfFailed);

                if (_semaphore.CurrentCount == _maxDegreeOfParalellism + 1)
                    CompleteLoopNow();
            }

            public void CompleteLoopNow()
            {
                if (_exceptionList?.Count > 0)
                {
                    bool lockTaken = false;
                    while (!lockTaken)
                        _exceptionListLock.Enter(ref lockTaken);
                    try
                    {
                        _completionTcs.SetException(_exceptionList);
                    }
                    finally
                    {
                        _exceptionListLock.Exit(useMemoryBarrier: false);
                    }
                }
                else
                {
                    _completionTcs.SetResult(null);
                }

                try
                {
                    if (_semaphore != null)
                        _semaphore.Dispose();
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParalellism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParalellism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (asyncItemAction == null)
                throw new ArgumentNullException(nameof(asyncItemAction));

            var context = new ParallelForEachContext(maxDegreeOfParalellism, breakLoopOnException);

            Task.Run(
                async () =>
                {
                    try
                    {
                        using (var enumerator = await collection.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
                        {
                            var itemIndex = 0L;

                            while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                            {
                                if (context.IsLoopBreakRequested)
                                    break;

                                await context.OnStartOperationAsync(cancellationToken).ConfigureAwait(false);

                                Task itemActionTask = null;
                                try
                                {
                                    itemActionTask = asyncItemAction(enumerator.Current, itemIndex);
                                }
                                // there is no guarantee that task is executed asynchronoyusly, so it can throw right away
                                catch (Exception ex)
                                {
                                    ex.Data["ForEach.Index"] = itemIndex;
                                    context.OnOperationComplete(ex);
                                }

                                if (itemActionTask != null)
                                {
                                    var capturedItemIndex = itemIndex;
#pragma warning disable CS4014  // Justification: not awaited by design
                                    itemActionTask.ContinueWith(
                                        task =>
                                        {
                                            Exception ex = null;
                                            if (task.IsFaulted)
                                            {
                                                ex = task.Exception;

                                                var aggEx = ex as AggregateException;
                                                if (aggEx != null && aggEx.InnerExceptions.Count == 1)
                                                    ex = aggEx.InnerException;

                                                ex.Data["ForEach.Index"] = capturedItemIndex;
                                            }

                                            context.OnOperationComplete(ex);
                                        });
#pragma warning restore CS4014
                                }

                                itemIndex++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        context.AddException(ex);
                    }
                    finally
                    {
                        if (context.IsLoopBreakRequested)
                            context.CompleteLoopNow();
                        else
                            context.OnOperationComplete();
                    }
                });

            return context.CompletionTask;
        }

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParalellism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParalellism,
            CancellationToken cancellationToken = default(CancellationToken))
            => collection.ParallelForEachAsync(
                asyncItemAction,
                maxDegreeOfParalellism,
                /*breakLoopOnException:*/false,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            CancellationToken cancellationToken = default(CancellationToken))
            => collection.ParallelForEachAsync(
                asyncItemAction,
                /*maxDegreeOfParalellism:*/0,
                /*breakLoopOnException:*/false,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParalellism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParalellism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default(CancellationToken))
            => collection.ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParalellism,
                breakLoopOnException,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParalellism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParalellism,
            CancellationToken cancellationToken = default(CancellationToken))
            => collection.ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParalellism,
                /*breakLoopOnException:*/false,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            CancellationToken cancellationToken = default(CancellationToken))
            => collection.ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                /*maxDegreeOfParalellism:*/0,
                /*breakLoopOnException:*/false,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParalellism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParalellism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default(CancellationToken))
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                asyncItemAction,
                maxDegreeOfParalellism,
                breakLoopOnException,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParalellism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParalellism,
            CancellationToken cancellationToken = default(CancellationToken))
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                asyncItemAction,
                maxDegreeOfParalellism,
                /*breakLoopOnException:*/false,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            CancellationToken cancellationToken = default(CancellationToken))
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                asyncItemAction,
                /*maxDegreeOfParalellism:*/0,
                /*breakLoopOnException:*/false,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParalellism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParalellism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default(CancellationToken))
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParalellism,
                breakLoopOnException,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParalellism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParalellism,
            CancellationToken cancellationToken = default(CancellationToken))
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParalellism,
                /*breakLoopOnException:*/false,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            CancellationToken cancellationToken = default(CancellationToken))
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                /*maxDegreeOfParalellism:*/0,
                /*breakLoopOnException:*/false,
                cancellationToken);
    }
}
