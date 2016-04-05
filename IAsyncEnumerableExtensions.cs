using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    /// <summary>
    /// Extension methods for <see cref="IAsyncEnumerable"/> interface
    /// </summary>
    public static class IAsyncEnumerableExtensions
    {
        /// <summary>
        /// Creates a list of elements asynchronously from the enumerable source
        /// </summary>
        /// <typeparam name="T">The type of the elements of source</typeparam>
        /// <param name="source">The collection of elements</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation</param>
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            var resultList = new List<T>();
            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken)) {
                while (await enumerator.MoveNextAsync(cancellationToken)) {
                    resultList.Add(enumerator.Current);
                }
            }
            return resultList;
        }

        /// <summary>
        /// Creates an array of elements asynchronously from the enumerable source
        /// </summary>
        /// <typeparam name="T">The type of the elements of source</typeparam>
        /// <param name="source">The collection of elements</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation</param>
        public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            var list = await source.ToListAsync(cancellationToken);
            return list.ToArray();
        }
    }
}
