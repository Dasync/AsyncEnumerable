using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    /// <summary>
    /// Helps to enumerate items in a collection asynchronously
    /// </summary>
    public sealed class AsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        /// <summary>
        /// This exception is thrown when you call <see cref="Yield.Break"/>
        /// </summary>
        public sealed class AsyncEnumerationCanceledException : OperationCanceledException { }

        /// <summary>
        /// The asynchronous version of the 'yield' construction
        /// </summary>
        public sealed class Yield
        {
            private TaskCompletionSource<bool> _resumeTCS;
            private TaskCompletionSource<T> _yieldTCS;

            /// <summary>
            /// Gets the cancellation token that was passed to the <see cref="MoveNextAsync"/> method
            /// </summary>
            public CancellationToken CancellationToken { get; private set; }

            /// <summary>
            /// Yields an item asynchronously (similar to 'yield return' statement)
            /// </summary>
            /// <param name="item">The item of the collection to yield</param>
            /// <returns>Returns a Task which tells if when you can continue to yield the next item</returns>
            public Task ReturnAsync(T item)
            {
                _resumeTCS = new TaskCompletionSource<bool>();
                _yieldTCS.TrySetResult(item);
                return _resumeTCS.Task;
            }

            /// <summary>
            /// Stops iterating items in the collection (similar to 'yield break' statement)
            /// </summary>
            /// <exception cref="AsyncEnumerationCanceledException">Always throws this exception to stop the enumeration task</exception>
            public void Break()
            {
                SetCanceled();
                throw new AsyncEnumerationCanceledException();
            }

            internal void SetComplete()
            {
                _yieldTCS.TrySetCanceled();
                IsComplete = true;
            }

            internal void SetCanceled()
            {
                SetComplete();
            }

            internal void SetFailed(Exception ex)
            {
                _yieldTCS.TrySetException(ex);
                IsComplete = true;
            }

            internal Task<T> OnMoveNext(CancellationToken cancellationToken)
            {
                if (!IsComplete) {
                    _yieldTCS = new TaskCompletionSource<T>();
                    CancellationToken = cancellationToken;
                    if (_resumeTCS != null)
                        _resumeTCS.SetResult(true);
                }
                return _yieldTCS.Task;
            }

            internal void Finilize()
            {
                SetCanceled();
            }

            internal bool IsComplete { get; set; }
        }

        private Func<Yield, Task> _enumerationFunction;
        private bool _oneTimeUse;
        private Yield _yield;
        private T _current;
        private Task _enumerationTask;
        private Exception _enumerationException;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="enumerationFunction">A function that enumerates items in a collection asynchronously</param>
        public AsyncEnumerator(Func<Yield, Task> enumerationFunction)
            : this(enumerationFunction, oneTimeUse: false)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="enumerationFunction">A function that enumerates items in a collection asynchronously</param>
        /// <param name="oneTimeUse">When True the enumeration can be performed once only and Reset method is not allowed</param>
        public AsyncEnumerator(Func<Yield, Task> enumerationFunction, bool oneTimeUse)
        {
            _enumerationFunction = enumerationFunction;
            _oneTimeUse = oneTimeUse;
            ClearState();
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator
        /// </summary>
        public T Current
        {
            get
            {
                if (_enumerationTask == null)
                    throw new InvalidOperationException("Call MoveNext() or MoveNextAsync() before accessing the Current item");
                return _current;
            }
            private set
            {
                _current = value;
            }
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Advances the enumerator to the next element of the collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the enumeration</param>
        /// <returns>Returns a Task that does transition to the next element. The result of the task is True if the enumerator was successfully advanced to the next element, or False if the enumerator has passed the end of the collection.</returns>
        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_enumerationException != null) {
                var tcs = new TaskCompletionSource<bool>();
                tcs.SetException(_enumerationException);
                return tcs.Task;
            }
            var moveNextTask = _yield.OnMoveNext(cancellationToken).ContinueWith(OnMoveNextComplete, _yield);
            if (_enumerationTask == null)
                _enumerationTask = _enumerationFunction(_yield).ContinueWith(OnEnumerationComplete, _yield);
            return moveNextTask;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            return MoveNextAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sets the enumerator to its initial position asynchronously, which is before the first element in the collection
        /// </summary>
        public Task ResetAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Reset();
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection
        /// </summary>
        public void Reset()
        {
            if (_oneTimeUse)
                throw new InvalidOperationException("The enumeration can be performed once only");
            ClearState();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        public void Dispose()
        {
            ClearState();
        }

        private void ClearState()
        {
            if (_yield != null)
                _yield.Finilize();

            _yield = new Yield();
            _enumerationTask = null;
            _enumerationException = null;
        }

        private bool OnMoveNextComplete(Task<T> task, object state)
        {
            var yield = (Yield)state;
            if (yield.IsComplete) {
                return false;
            }

            if (task.IsFaulted) {
                _enumerationException = task.Exception;
                throw _enumerationException;
            } else if (task.IsCanceled) {
                return false;
            } else {
                Current = task.Result;
                return true;
            }
        }

        private static void OnEnumerationComplete(Task task, object state)
        {
            var yield = (Yield)state;
            if (task.IsFaulted) {
                if (task.Exception is AsyncEnumerationCanceledException) {
                    yield.SetCanceled();
                } else {
                    yield.SetFailed(task.Exception);
                }
            } else if (task.IsCanceled) {
                yield.SetCanceled();
            } else {
                yield.SetComplete();
            }
        }
    }
}
