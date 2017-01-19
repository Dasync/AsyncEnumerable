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
        /// The asynchronous version of the 'yield' construction
        /// </summary>
        public sealed class Yield
        {
            private TaskCompletionSource<bool> _resumeEnumerationTcs; // Can be of any type - there is no non-generic version of the TaskCompletionSource
            private TaskCompletionSource<bool> _moveNextCompleteTcs;
            private AsyncEnumerator<T> _enumerator;
            private bool _isComplete;

            internal Yield(AsyncEnumerator<T> enumerator)
            {
                _enumerator = enumerator;
            }

            /// <summary>
            /// Gets the cancellation token that was passed to the <see cref="MoveNextAsync"/> method
            /// </summary>
            public CancellationToken CancellationToken { get; private set; }

            /// <summary>
            /// Yields an item asynchronously (similar to 'yield return' statement)
            /// </summary>
            /// <param name="item">The item of the collection to yield</param>
            /// <returns>Returns a Task which tells if when you can continue to yield the next item</returns>
#pragma warning disable AsyncMethodMustTakeCancellationToken // Does not take a CancellationToken by design
            public Task ReturnAsync(T item)
#pragma warning restore AsyncMethodMustTakeCancellationToken
            {
                _resumeEnumerationTcs = new TaskCompletionSource<bool>();
                _enumerator.Current = item;
                _moveNextCompleteTcs.TrySetResult(true);
                return _resumeEnumerationTcs.Task;
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
                _isComplete = true;
                _moveNextCompleteTcs.TrySetResult(false);
            }

            internal void SetCanceled()
            {
                _isComplete = true;
                _resumeEnumerationTcs?.TrySetException(new AsyncEnumerationCanceledException());
                _moveNextCompleteTcs.TrySetCanceled();
            }

            internal void SetFailed(Exception ex)
            {
                _isComplete = true;
                _moveNextCompleteTcs.TrySetException(ex.GetBaseException());
            }

            internal Task<bool> OnMoveNext(CancellationToken cancellationToken)
            {
                if (!_isComplete)
                {
                    CancellationToken = cancellationToken;
                    _moveNextCompleteTcs = new TaskCompletionSource<bool>();
                    _resumeEnumerationTcs?.SetResult(true);
                }

                return _moveNextCompleteTcs.Task;
            }
        }

        private static readonly Action<Task, object> OnEnumerationCompleteAction = OnEnumerationComplete;

        private Func<Yield, Task> _enumerationFunction;
        private bool _oneTimeUse;
        private Yield _yield;
        private T _current;
        private Task _enumerationTask;

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
                    throw new InvalidOperationException("Call MoveNextAsync() or MoveNext() before accessing the Current item");
                return _current;
            }
            internal set
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
            var moveNextCompleteTask = _yield.OnMoveNext(cancellationToken);
            if (_enumerationTask == null)
                _enumerationTask = _enumerationFunction(_yield).ContinueWith(OnEnumerationCompleteAction, _yield, TaskContinuationOptions.ExecuteSynchronously);
            return moveNextCompleteTask;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection
        /// </summary>
        /// <returns></returns>
        bool IEnumerator.MoveNext()
        {
            return MoveNextAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sets the enumerator to its initial position asynchronously, which is before the first element in the collection
        /// </summary>
        public Task ResetAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Reset();
            return AsyncEnumerable.CompletedTask;
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
            _yield?.SetCanceled();
            _yield = new Yield(this);
            _enumerationTask = null;
        }

        private static void OnEnumerationComplete(Task task, object state)
        {
            var yield = (Yield)state;
            if (task.IsFaulted)
            {
                if (task.Exception.GetBaseException() is AsyncEnumerationCanceledException)
                {
                    yield.SetCanceled();
                }
                else
                {
                    yield.SetFailed(task.Exception);
                }
            }
            else if (task.IsCanceled)
            {
                yield.SetCanceled();
            }
            else
            {
                yield.SetComplete();
            }
        }
    }
}
