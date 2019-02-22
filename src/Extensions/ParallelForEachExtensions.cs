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
            private readonly int _maxDegreeOfParallelism;
            private readonly bool _breakLoopOnException;
            private readonly bool _gracefulBreak;
            private CancellationToken _cancellationToken;
            private CancellationTokenRegistration _cancellationTokenRegistration;

            public ParallelForEachContext(int maxDegreeOfParallelism, bool breakLoopOnException, bool gracefulBreak, CancellationToken cancellationToken)
            {
                if (maxDegreeOfParallelism < 0)
                    throw new ArgumentException($"The maximum degree of parallelism must be a non-negative number, but got {maxDegreeOfParallelism}", nameof(maxDegreeOfParallelism));
                if (maxDegreeOfParallelism == 0)
                    maxDegreeOfParallelism = Environment.ProcessorCount - 1;
                if (maxDegreeOfParallelism <= 0)
                    maxDegreeOfParallelism = 1;

                _semaphore = new SemaphoreSlim(initialCount: maxDegreeOfParallelism, maxCount: maxDegreeOfParallelism + 1);
                _completionTcs = new TaskCompletionSource<object>();
                _exceptionListLock = new SpinLock(enableThreadOwnerTracking: false);
                _maxDegreeOfParallelism = maxDegreeOfParallelism;
                _breakLoopOnException = breakLoopOnException;
                _gracefulBreak = gracefulBreak;

                _cancellationToken = cancellationToken;
                if (_cancellationToken.CanBeCanceled)
                    _cancellationTokenRegistration = _cancellationToken.Register(OnCancelRequested, useSynchronizationContext: false);
            }

            public Task CompletionTask => _completionTcs.Task;

            public volatile bool IsLoopBreakRequested;

            public void AddException(Exception ex)
            {
                if (_cancellationToken.IsCancellationRequested && ex is OperationCanceledException)
                    return;

                bool lockTaken = false;
                while (!lockTaken)
                    _exceptionListLock.Enter(ref lockTaken);
                try
                {
                    if (_exceptionList == null)
                        _exceptionList = new List<Exception>();
                    _exceptionList.Add(ex);
                }
                finally
                {
                    _exceptionListLock.Exit(useMemoryBarrier: false);
                }
            }

            public List<Exception> ReadExceptions()
            {
                bool lockTaken = false;
                while (!lockTaken)
                    _exceptionListLock.Enter(ref lockTaken);
                try
                {
                    return _exceptionList;
                }
                finally
                {
                    _exceptionList = null;
                    _exceptionListLock.Exit(useMemoryBarrier: false);
                }
            }

            public Task OnStartOperationAsync(CancellationToken cancellationToken)
            {
                return _semaphore.WaitAsync(cancellationToken);
            }

            public void OnOperationComplete(Exception exceptionIfFailed = null)
            {
                if (exceptionIfFailed != null)
                {
                    AddException(exceptionIfFailed);

                    if (_breakLoopOnException)
                        IsLoopBreakRequested = true;
                }

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

                if ((_semaphore.CurrentCount == _maxDegreeOfParallelism + 1) || (IsLoopBreakRequested && !_gracefulBreak))
                    CompleteLoopNow();
            }

            public void CompleteLoopNow()
            {
                _cancellationTokenRegistration.Dispose();

                try
                {
                    if (_semaphore != null)
                        _semaphore.Dispose();
                }
                catch
                {
                }

                var exceptions = ReadExceptions();
                var aggregatedException = exceptions?.Count > 0 ? new ParallelForEachException(exceptions) : null;

                if (_cancellationToken.IsCancellationRequested)
                {
                    _completionTcs.TrySetException(
                        new OperationCanceledException(
                            new OperationCanceledException().Message,
                            aggregatedException,
                            _cancellationToken));
                }
                else if (exceptions?.Count > 0)
                {
                    _completionTcs.TrySetException(aggregatedException);
                }
                else
                {
                    _completionTcs.TrySetResult(null);
                }
            }

            private void OnCancelRequested()
            {
                IsLoopBreakRequested = true;

                if (!_gracefulBreak)
                    CompleteLoopNow();
            }
        }

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="gracefulBreak">If True (the default behavior), waits on completion for all started tasks when the loop breaks due to cancellation or an exception</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            bool gracefulBreak,
            CancellationToken cancellationToken = default)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (asyncItemAction == null)
                throw new ArgumentNullException(nameof(asyncItemAction));

            var context = new ParallelForEachContext(maxDegreeOfParallelism, breakLoopOnException, gracefulBreak, cancellationToken);

            Task.Run(
                async () =>
                {
                    try
                    {
#if NETCOREAPP3_0
                        var enumerator = collection.GetAsyncEnumerator(cancellationToken);
                        try
#else
                        using (var enumerator = await collection.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
#endif
                        {
                            var itemIndex = 0L;

#if NETCOREAPP3_0
                            while (await enumerator.MoveNextAsync())
#else
                            while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
#endif
                            {
                                if (context.IsLoopBreakRequested)
                                    break;

                                await context.OnStartOperationAsync(cancellationToken).ConfigureAwait(false);

                                if (context.IsLoopBreakRequested)
                                {
                                    context.OnOperationComplete();
                                    break;
                                }

                                Task itemActionTask = null;
                                try
                                {
                                    itemActionTask = asyncItemAction(enumerator.Current, itemIndex);
                                }
                                // there is no guarantee that task is executed asynchronously, so it can throw right away
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

                                                if (ex is AggregateException aggEx && aggEx.InnerExceptions.Count == 1)
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
#if NETCOREAPP3_0
                        finally
                        {
                            await enumerator.DisposeAsync();
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        context.AddException(ex);
                    }
                    finally
                    {
                        context.OnOperationComplete();
                    }
                });

            return context.CompletionTask;
        }

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="gracefulBreak">If True (the default behavior), waits on completion for all started tasks when the loop breaks due to cancellation or an exception</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerator<T> enumerator,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            bool gracefulBreak,
            CancellationToken cancellationToken = default)
        {
            if (enumerator == null)
                throw new ArgumentNullException(nameof(enumerator));
            if (asyncItemAction == null)
                throw new ArgumentNullException(nameof(asyncItemAction));

            var context = new ParallelForEachContext(maxDegreeOfParallelism, breakLoopOnException, gracefulBreak, cancellationToken);

            Task.Run(
                async () =>
                {
                    try
                    {
                        var itemIndex = 0L;

#if NETCOREAPP3_0
                        while (await enumerator.MoveNextAsync())
#else
                        while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
#endif
                        {
                            if (context.IsLoopBreakRequested)
                                break;

                            await context.OnStartOperationAsync(cancellationToken).ConfigureAwait(false);

                            if (context.IsLoopBreakRequested)
                            {
                                context.OnOperationComplete();
                                break;
                            }

                            Task itemActionTask = null;
                            try
                            {
                                itemActionTask = asyncItemAction(enumerator.Current, itemIndex);
                            }
                            // there is no guarantee that task is executed asynchronously, so it can throw right away
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

                                            if (ex is AggregateException aggEx && aggEx.InnerExceptions.Count == 1)
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
                    catch (Exception ex)
                    {
                        context.AddException(ex);
                    }
                    finally
                    {
#if NETCOREAPP3_0
                        await enumerator.DisposeAsync();
#else
                        enumerator.Dispose();
#endif
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
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default)
            => collection.ParallelForEachAsync(
                asyncItemAction,
                maxDegreeOfParallelism,
                breakLoopOnException,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerator<T> enumerator,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default)
            => enumerator.ParallelForEachAsync(
                asyncItemAction,
                maxDegreeOfParallelism,
                breakLoopOnException,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken = default)
            => collection.ParallelForEachAsync(
                asyncItemAction,
                maxDegreeOfParallelism,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerator<T> enumerator,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken = default)
            => enumerator.ParallelForEachAsync(
                asyncItemAction,
                maxDegreeOfParallelism,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            CancellationToken cancellationToken = default)
            => collection.ParallelForEachAsync(
                asyncItemAction,
                /*maxDegreeOfParallelism:*/0,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerator<T> enumerator,
            Func<T, long, Task> asyncItemAction,
            CancellationToken cancellationToken = default)
            => enumerator.ParallelForEachAsync(
                asyncItemAction,
                /*maxDegreeOfParallelism:*/0,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default)
            => collection.ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParallelism,
                breakLoopOnException,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerator<T> enumerator,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default)
            => enumerator.ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParallelism,
                breakLoopOnException,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="gracefulBreak">If True (the default behavior), waits on completion for all started tasks when the loop breaks due to cancellation or an exception</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            bool gracefulBreak,
            CancellationToken cancellationToken = default)
            => collection.ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParallelism,
                breakLoopOnException,
                gracefulBreak,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="gracefulBreak">If True (the default behavior), waits on completion for all started tasks when the loop breaks due to cancellation or an exception</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerator<T> enumerator,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            bool gracefulBreak,
            CancellationToken cancellationToken = default)
            => enumerator.ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParallelism,
                breakLoopOnException,
                gracefulBreak,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken = default)
            => collection.ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParallelism,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerator<T> enumerator,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken = default)
            => enumerator.ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParallelism,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            CancellationToken cancellationToken = default)
            => collection.ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                /*maxDegreeOfParallelism:*/0,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IAsyncEnumerator<T> enumerator,
            Func<T, Task> asyncItemAction,
            CancellationToken cancellationToken = default)
            => enumerator.ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                /*maxDegreeOfParallelism:*/0,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default)
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                asyncItemAction,
                maxDegreeOfParallelism,
                breakLoopOnException,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerator<T> enumerator,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default)
            => enumerator.ToAsyncEnumerator(runSynchronously: true).ParallelForEachAsync(
                asyncItemAction,
                maxDegreeOfParallelism,
                breakLoopOnException,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken = default)
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                asyncItemAction,
                maxDegreeOfParallelism,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerator<T> enumerator,
            Func<T, long, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken = default)
            => enumerator.ToAsyncEnumerator(runSynchronously: true).ParallelForEachAsync(
                asyncItemAction,
                maxDegreeOfParallelism,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, long, Task> asyncItemAction,
            CancellationToken cancellationToken = default)
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                asyncItemAction,
                /*maxDegreeOfParallelism:*/0,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item, where first argument is the item and second argument is item's index in the collection</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerator<T> enumerator,
            Func<T, long, Task> asyncItemAction,
            CancellationToken cancellationToken = default)
            => enumerator.ToAsyncEnumerator(runSynchronously: true).ParallelForEachAsync(
                asyncItemAction,
                /*maxDegreeOfParallelism:*/0,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default)
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParallelism,
                breakLoopOnException,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="breakLoopOnException">Set to True to stop processing items when first exception occurs. The result <see cref="AggregateException"/> might contain several exceptions though when faulty tasks finish at the same time.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerator<T> enumerator,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            bool breakLoopOnException,
            CancellationToken cancellationToken = default)
            => enumerator.ToAsyncEnumerator(runSynchronously: true).ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParallelism,
                breakLoopOnException,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken = default)
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParallelism,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="maxDegreeOfParallelism">Maximum items to schedule processing in parallel. The actual concurrency level depends on TPL settings. Set to 0 to choose a default value based on processor count.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerator<T> enumerator,
            Func<T, Task> asyncItemAction,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken = default)
            => enumerator.ToAsyncEnumerator(runSynchronously: true).ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                maxDegreeOfParallelism,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="collection">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerable<T> collection,
            Func<T, Task> asyncItemAction,
            CancellationToken cancellationToken = default)
            => collection.ToAsyncEnumerable<T>(runSynchronously: true).ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                /*maxDegreeOfParallelism:*/0,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);

        /// <summary>
        /// Invokes an asynchronous action on each item in the collection in parallel
        /// </summary>
        /// <typeparam name="T">The type of an item</typeparam>
        /// <param name="enumerator">The collection of items to perform actions on</param>
        /// <param name="asyncItemAction">An asynchronous action to perform on the item</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ParallelForEachException">Wraps any exception(s) that occurred inside <paramref name="asyncItemAction"/></exception>
        /// <exception cref="OperationCanceledException">Thrown when the loop is canceled with <paramref name="cancellationToken"/></exception>
        public static Task ParallelForEachAsync<T>(
            this IEnumerator<T> enumerator,
            Func<T, Task> asyncItemAction,
            CancellationToken cancellationToken = default)
            => enumerator.ToAsyncEnumerator(runSynchronously: true).ParallelForEachAsync(
                (item, index) => asyncItemAction(item),
                /*maxDegreeOfParallelism:*/0,
                /*breakLoopOnException:*/false,
                /*gracefulBreak:*/true,
                cancellationToken);
    }
}
