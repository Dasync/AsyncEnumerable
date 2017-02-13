using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    /// <summary>
    /// Supports a simple asynchronous iteration over a non-generic collection
    /// </summary>
    public interface IAsyncEnumerator : IDisposable
    {
        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        object Current { get; }

        /// <summary>
        /// Advances the enumerator to the next element of the collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the enumeration</param>
        /// <returns>Returns a Task that does transition to the next element. The result of the task is True if the enumerator was successfully advanced to the next element, or False if the enumerator has passed the end of the collection.</returns>
        Task<bool> MoveNextAsync(CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// Supports a simple asynchronous iteration over a collection of typed items
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    public interface IAsyncEnumerator<out T> : IAsyncEnumerator
    {
        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        new T Current { get; }
    }
}
