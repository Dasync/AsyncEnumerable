using System.Collections.Async.Internals;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    /// <summary>
    /// Base abstract class that implements <see cref="IAsyncEnumerable"/>.
    /// Use concrete implementation <see cref="AsyncEnumerable{T}"/> or <see cref="AsyncEnumerableWithState{TItem, TState}"/>.
    /// </summary>
    public abstract class AsyncEnumerable : IAsyncEnumerable
    {
        /// <summary>
        /// Returns pre-cached empty collection
        /// </summary>
        public static IAsyncEnumerable<T> Empty<T>() => AsyncEnumerable<T>.Empty;

        Task<IAsyncEnumerator> IAsyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Helps to enumerate items in a collection asynchronously
    /// </summary>
    /// <example>
    /// <code>
    /// IAsyncEnumerable&lt;int&gt; ProduceNumbers(int start, int end)
    /// {
    ///   return new AsyncEnumerable&lt;int&gt;(async yield => {
    ///     for (int number = start; number &lt;= end; number++)
    ///       await yield.ReturnAsync(number);
    ///   });
    /// }
    /// 
    /// async Task ConsumeAsync()
    /// {
    ///   var asyncEnumerableCollection = ProduceNumbers(start: 1, end: 10);
    ///   await asyncEnumerableCollection.ForEachAsync(async number => {
    ///     await Console.Out.WriteLineAsync(number);
    ///   });
    /// }
    /// </code>
    /// </example>
    public class AsyncEnumerable<T> : AsyncEnumerable, IAsyncEnumerable<T>
    {
        private readonly Func<AsyncEnumerator<T>.Yield, Task> _enumerationFunction;

        /// <summary>
        /// A pre-cached empty collection
        /// </summary>
        public readonly static IAsyncEnumerable<T> Empty = new EmptyAsyncEnumerable<T>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="enumerationFunction">A function that enumerates items in a collection asynchronously</param>
        public AsyncEnumerable(Func<AsyncEnumerator<T>.Yield, Task> enumerationFunction)
        {
            _enumerationFunction = enumerationFunction;
        }

        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        public virtual Task<IAsyncEnumerator<T>> GetAsyncEnumeratorAsync(CancellationToken cancellationToken = default)
        {
            var enumerator = new AsyncEnumerator<T>(_enumerationFunction);
            return Task.FromResult<IAsyncEnumerator<T>>(enumerator);
        }

        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        async Task<IAsyncEnumerator> IAsyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken cancellationToken)
        {
            return await GetAsyncEnumeratorAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Similar to <see cref="AsyncEnumerable{T}"/>, but allows you to pass a state object into the enumeration function, what can be
    /// used for performance optimization, so don't have to create a delegate on the fly every single time you create the enumerator.
    /// </summary>
    /// <typeparam name="TItem">Type of items returned by </typeparam>
    /// <typeparam name="TState">Type of the state object</typeparam>
    public class AsyncEnumerableWithState<TItem, TState> : AsyncEnumerable, IAsyncEnumerable<TItem>
    {
        private readonly Func<AsyncEnumerator<TItem>.Yield, TState, Task> _enumerationFunction;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="enumerationFunction">A function that enumerates items in a collection asynchronously</param>
        /// <param name="state">A state object that is passed to the <paramref name="enumerationFunction"/></param>
        public AsyncEnumerableWithState(Func<AsyncEnumerator<TItem>.Yield, TState, Task> enumerationFunction, TState state)
        {
            _enumerationFunction = enumerationFunction;
            State = state;
        }

        /// <summary>
        /// A user state that gets passed into the enumeration function.
        /// </summary>
        protected TState State { get; }

        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        public virtual Task<IAsyncEnumerator<TItem>> GetAsyncEnumeratorAsync(CancellationToken cancellationToken = default)
        {
            var enumerator = new AsyncEnumeratorWithState<TItem, TState>(_enumerationFunction, State);
            return Task.FromResult<IAsyncEnumerator<TItem>>(enumerator);
        }

        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        Task<IAsyncEnumerator> IAsyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken cancellationToken)
        {
            var enumerator = new AsyncEnumeratorWithState<TItem, TState>(_enumerationFunction, State);
            return Task.FromResult<IAsyncEnumerator>(enumerator);
        }
    }
}