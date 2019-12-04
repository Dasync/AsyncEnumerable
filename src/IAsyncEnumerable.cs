using System.Threading;

namespace Dasync.Collections
{
    /// <summary>
    /// Exposes an asynchronous enumerator, which supports a simple iteration over a non-generic collection
    /// </summary>
    public interface IAsyncEnumerable
    {
        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        IAsyncEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default);
    }

#if !NETSTANDARD2_1 && !NETSTANDARD2_0 && !NET461
    /// <summary>
    /// Exposes the asynchronous enumerator, which supports a simple iteration over a collection of typed items
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    public interface IAsyncEnumerable<out T> : IAsyncEnumerable
    {
        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        new IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
    }
#endif
}
