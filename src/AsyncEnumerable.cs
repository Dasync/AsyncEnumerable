using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections.Internals;

namespace Dasync.Collections
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

        IAsyncEnumerator IAsyncEnumerable.GetAsyncEnumerator(CancellationToken cancellationToken)
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
    public class AsyncEnumerable<T> : AsyncEnumerable, IAsyncEnumerable, IAsyncEnumerable<T>
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
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new AsyncEnumerator<T>(_enumerationFunction) { MasterCancellationToken = cancellationToken };

        IAsyncEnumerator IAsyncEnumerable.GetAsyncEnumerator(CancellationToken cancellationToken)
            => new AsyncEnumerator<T>(_enumerationFunction) { MasterCancellationToken = cancellationToken };
    }

    /// <summary>
    /// Similar to <see cref="AsyncEnumerable{T}"/>, but allows you to pass a state object into the enumeration function, what can be
    /// used for performance optimization, so don't have to create a delegate on the fly every single time you create the enumerator.
    /// </summary>
    /// <typeparam name="TItem">Type of items returned by </typeparam>
    /// <typeparam name="TState">Type of the state object</typeparam>
    public class AsyncEnumerableWithState<TItem, TState> : AsyncEnumerable, IAsyncEnumerable, IAsyncEnumerable<TItem>
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
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        public virtual IAsyncEnumerator<TItem> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new AsyncEnumeratorWithState<TItem, TState>(_enumerationFunction, State)
                { MasterCancellationToken = cancellationToken };

        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        IAsyncEnumerator IAsyncEnumerable.GetAsyncEnumerator(CancellationToken cancellationToken)
            => new AsyncEnumeratorWithState<TItem, TState>(_enumerationFunction, State)
                { MasterCancellationToken = cancellationToken };
    }
}