﻿using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    /// <summary>
    /// Exposes an asynchronous enumerator, which supports a simple iteration over a non-generic collection
    /// </summary>
    public interface IAsyncEnumerable
    {
#if NETCOREAPP3_0
        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        IAsyncEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default);
#else
        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        Task<IAsyncEnumerator> GetAsyncEnumeratorAsync(CancellationToken cancellationToken = default);
#endif
    }

#if !NETCOREAPP3_0
    /// <summary>
    /// Exposes the asynchronous enumerator, which supports a simple iteration over a collection of typed items
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    public interface IAsyncEnumerable<T> : IAsyncEnumerable
    {
        /// <summary>
        /// Creates an enumerator that iterates through a collection asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel creation of the enumerator in case if it takes a lot of time</param>
        /// <returns>Returns a task with the created enumerator as result on completion</returns>
        new Task<IAsyncEnumerator<T>> GetAsyncEnumeratorAsync(CancellationToken cancellationToken = default);
    }
#endif
}
