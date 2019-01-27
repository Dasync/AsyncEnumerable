using System.Collections.Async.Internals;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    /// <summary>
    /// Base type for <see cref="AsyncEnumerator{T}"/> and <see cref="AsyncEnumeratorWithState{TItem, TState}"/>
    /// </summary>
    public abstract class AsyncEnumerator
    {
        /// <summary>
        /// Returns an empty <see cref="IAsyncEnumerator{T}"/>. Safe to use by multiple threads.
        /// </summary>
        public static IAsyncEnumerator<T> Empty<T>() => AsyncEnumerator<T>.Empty;
    }

    /// <summary>
    /// Helps to enumerate items in a collection asynchronously.
    /// Provides exactly the same functionality as <see cref="AsyncEnumerator{T}"/>,
    /// but allows to pass a user state object in the enumeration function,
    /// what can be used for performance optimization.
    /// </summary>
    public class AsyncEnumeratorWithState<TItem, TState> : CurrentValueContainer<TItem>, IAsyncEnumerator<TItem>
    {
        private static readonly Action<Task, object> OnEnumerationCompleteAction = OnEnumerationComplete;

        private Func<AsyncEnumerator<TItem>.Yield, TState, Task> _enumerationFunction;
        private Action<TState> _onDisposeAction;
        private AsyncEnumerator<TItem>.Yield _yield;
        private Task _enumerationTask;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="enumerationFunction">A function that enumerates items in a collection asynchronously</param>
        /// <param name="state">Any state object that is passed to the <paramref name="enumerationFunction"/></param>
        /// <param name="onDispose">Optional action that gets invoked on Dispose()</param>
        public AsyncEnumeratorWithState(
            Func<AsyncEnumerator<TItem>.Yield, TState, Task> enumerationFunction,
            TState state,
            Action<TState> onDispose = null)
        {
            _enumerationFunction = enumerationFunction ?? throw new ArgumentNullException(nameof(enumerationFunction));
            _onDisposeAction = onDispose;
            State = state;

            // If dispose action has not been defined and enumeration has not been started, there is nothing to finilize.
            if (onDispose == null)
                GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~AsyncEnumeratorWithState()
        {
            Dispose(manualDispose: false);
        }

        /// <summary>
        /// A user state that gets passed into the enumeration function.
        /// </summary>
        protected TState State { get; }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator
        /// </summary>
        public virtual TItem Current
        {
            get
            {
                if (!HasCurrentValue)
                    throw new InvalidOperationException("Call MoveNextAsync() or MoveNext() before accessing the Current item");
                return CurrentValue;
            }
        }

        /// <summary>
        /// Tells if enumeration is complete. Returns True only after MoveNextAsync returns False.
        /// </summary>
        public bool IsEnumerationComplete => _yield != null && _yield.IsComplete;

        object IAsyncEnumerator.Current => Current;

        /// <summary>
        /// Advances the enumerator to the next element of the collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the enumeration</param>
        /// <returns>Returns a Task that does transition to the next element. The result of the task is True if the enumerator was successfully advanced to the next element, or False if the enumerator has passed the end of the collection.</returns>
        public virtual Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            if (_enumerationFunction == null)
                return TaskEx.False;

            if (_yield == null)
                _yield = new AsyncEnumerator<TItem>.Yield(this);

            var moveNextCompleteTask = _yield.OnMoveNext(cancellationToken);

            if (_enumerationTask == null)
            {
                // Register for finalization, which might be needed if caller
                // doesn't not finish the enumeration and does not call Dispose().
                GC.ReRegisterForFinalize(this);

                _enumerationTask =
                    _enumerationFunction(_yield, State)
                    .ContinueWith(OnEnumerationCompleteAction, this, TaskContinuationOptions.ExecuteSynchronously);
            }

            return moveNextCompleteTask;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        public void Dispose()
        {
            Dispose(manualDispose: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        /// <param name="manualDispose">True if called from Dispose() method, otherwise False - called by GC</param>
        protected virtual void Dispose(bool manualDispose)
        {
            if (manualDispose)
            {
                _yield?.SetCanceled();
            }
            else if (_yield != null && !_yield.IsComplete)
            {
                var yield = _yield;
                Task.Run(() => yield.SetCanceled()); // don't block the GC thread
            }

            _enumerationTask = null;
            _yield = null;

            _onDisposeAction?.Invoke(State);
            _onDisposeAction = null;
            _enumerationFunction = null;
        }

        private static void OnEnumerationComplete(Task task, object state)
        {
            var enumerator = (AsyncEnumeratorWithState<TItem, TState>)state;

            // When en enumeration is complete, there is nothing to dispose.
            GC.SuppressFinalize(enumerator);

            if (task.IsFaulted)
            {
                if (task.Exception.GetBaseException() is AsyncEnumerationCanceledException)
                {
                    enumerator._yield?.SetCanceled();
                }
                else
                {
                    enumerator._yield?.SetFailed(task.Exception);
                }
            }
            else if (task.IsCanceled)
            {
                enumerator._yield?.SetCanceled();
            }
            else
            {
                enumerator._yield?.SetComplete();
            }
        }
    }

    /// <summary>
    /// Helps to enumerate items in a collection asynchronously
    /// </summary>
    public class AsyncEnumerator<T> : AsyncEnumeratorWithState<T, AsyncEnumerator<T>.NoStateAdapter>
    {
        /// <summary>
        /// An empty <see cref="IAsyncEnumerator{T}"/>. Safe to use by multiple threads.
        /// </summary>
        public static readonly IAsyncEnumerator<T> Empty = new EmptyAsyncEnumerator<T>();

        /// <summary>
        /// The asynchronous version of the 'yield' construction
        /// </summary>
        public sealed class Yield
        {
            private TaskCompletionSource<bool> _resumeEnumerationTcs; // Can be of any type - there is no non-generic version of the TaskCompletionSource
            private TaskCompletionSource<bool> _moveNextCompleteTcs;
            private CurrentValueContainer<T> _currentValueContainer;
            private bool _isComplete;

            internal Yield(CurrentValueContainer<T> currentValueContainer)
            {
                _currentValueContainer = currentValueContainer;
            }

            /// <summary>
            /// Gets the cancellation token that was passed to the <see cref="IAsyncEnumerator.MoveNextAsync(CancellationToken)"/> method
            /// </summary>
            public CancellationToken CancellationToken { get; private set; }

            internal bool IsComplete => _isComplete;

            /// <summary>
            /// Yields an item asynchronously (similar to 'yield return' statement)
            /// </summary>
            /// <param name="item">The item of the collection to yield</param>
            /// <returns>Returns a Task which tells if when you can continue to yield the next item</returns>
#pragma warning disable AsyncMethodMustTakeCancellationToken // Does not take a CancellationToken by design
            public Task ReturnAsync(T item)
#pragma warning restore AsyncMethodMustTakeCancellationToken
            {
                TaskCompletionSource.Reset(ref _resumeEnumerationTcs);
                _currentValueContainer.CurrentValue = item;
                _moveNextCompleteTcs.TrySetResult(true);
                return _resumeEnumerationTcs.Task;
            }

            /// <summary>
            /// Stops iterating items in the collection (similar to 'yield break' statement)
            /// </summary>
            /// <exception cref="AsyncEnumerationCanceledException">Always throws this exception to stop the enumeration task</exception>
            public void Break()
            {
                SetComplete();
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
                if (!CancellationToken.IsCancellationRequested)
                    CancellationToken = CancellationTokenEx.Canceled;
                _resumeEnumerationTcs?.TrySetException(new AsyncEnumerationCanceledException());
                _moveNextCompleteTcs?.TrySetCanceled();
            }

            internal void SetFailed(Exception ex)
            {
                _isComplete = true;
                _moveNextCompleteTcs?.TrySetException(ex.GetBaseException());
            }

            internal Task<bool> OnMoveNext(CancellationToken cancellationToken)
            {
                if (!_isComplete)
                {
                    CancellationToken = cancellationToken;
                    TaskCompletionSource.Reset(ref _moveNextCompleteTcs);
                    _resumeEnumerationTcs?.TrySetResult(true);
                }

                return _moveNextCompleteTcs.Task;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="enumerationFunction">A function that enumerates items in a collection asynchronously</param>
        /// <param name="onDispose">Optional action that gets invoked on Dispose()</param>
        public AsyncEnumerator(Func<Yield, Task> enumerationFunction, Action onDispose = null)
            : base(
                  enumerationFunction: NoStateAdapter.Enumerate,
                  onDispose: onDispose == null ? null : NoStateAdapter.OnDispose,
                  state: new NoStateAdapter
                  {
                      EnumerationFunction = enumerationFunction,
                      DisposeAction = onDispose
                  })
        {
        }

        /// <summary>
        /// Internal implementation details
        /// </summary>
        [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
        public struct NoStateAdapter
        {
            internal Func<Yield, Task> EnumerationFunction;
            internal Action DisposeAction;

            internal static readonly Func<Yield, NoStateAdapter, Task> Enumerate = EnumerateWithNoStateAdapter;
            internal static readonly Action<NoStateAdapter> OnDispose = OnDisposeAdapter;

            private static Task EnumerateWithNoStateAdapter(Yield yield, NoStateAdapter state) => state.EnumerationFunction(yield);

            private static void OnDisposeAdapter(NoStateAdapter state) => state.DisposeAction?.Invoke();
        }
    }
}
