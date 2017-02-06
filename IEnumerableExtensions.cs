using System.Collections.Async;
using System.Collections.Async.Internals;
using System.Linq;
using System.Threading;

namespace System.Collections
{
    /// <summary>
    /// Converts generic IEnumerable to IAsyncEnumerable
    /// </summary>
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Creates <see cref="IAsyncEnumerable"/> adapter for <see cref="IEnumerable"/>
        /// </summary>
        /// <param name="enumerable">The instance of <see cref="IEnumerable"/> to convert</param>
        /// <param name="runSynchronously">If True the enumeration will be performed on the same thread, otherwise the MoveNext will be executed on a separate thread with Task.Run method</param>
        /// <returns>Returns an instance of <see cref="IAsyncEnumerable"/> implementation</returns>
        public static IAsyncEnumerable ToAsyncEnumerable(this IEnumerable enumerable, bool runSynchronously = true)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            return enumerable as IAsyncEnumerable ?? new AsyncEnumerableWrapper<object>(enumerable.Cast<object>(), runSynchronously);
        }
    }
}

namespace System.Collections.Generic
{
    /// <summary>
    /// Converts generic IEnumerable to IAsyncEnumerable
    /// </summary>
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Creates <see cref="IAsyncEnumerable{T}"/> adapter for <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <param name="enumerable">The instance of <see cref="IEnumerable{T}"/> to convert</param>
        /// <param name="runSynchronously">If True the enumeration will be performed on the same thread, otherwise the MoveNext will be executed on a separate thread with Task.Run method</param>
        /// <returns>Returns an instance of <see cref="IAsyncEnumerable{T}"/> implementation</returns>
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable, bool runSynchronously = true)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            return enumerable as IAsyncEnumerable<T> ?? new AsyncEnumerableWrapper<T>(enumerable, runSynchronously);
        }

        /// <summary>
        /// Creates <see cref="IAsyncEnumerator{T}"/> adapter for the enumerator of <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <param name="enumerable">The instance of <see cref="IEnumerable{T}"/> to convert</param>
        /// <param name="runSynchronously">If True the enumeration will be performed on the same thread, otherwise the MoveNext will be executed on a separate thread with Task.Run method</param>
        /// <returns>Returns an instance of <see cref="IAsyncEnumerable{T}"/> implementation</returns>
        public static IAsyncEnumerator<T> GetAsyncEnumerator<T>(this IEnumerable<T> enumerable, bool runSynchronously = true)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            var asyncEnumerable = enumerable as IAsyncEnumerable<T>;
            if (asyncEnumerable != null)
                return asyncEnumerable
                    .GetAsyncEnumeratorAsync(CancellationToken.None)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

            var enumerator = enumerable.GetEnumerator();
            return new AsyncEnumeratorWrapper<T>(enumerator, runSynchronously);
        }

        /// <summary>
        /// Creates <see cref="IAsyncEnumerator{T}"/> adapter for <see cref="IEnumerator{T}"/>
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <param name="enumerator">The instance of <see cref="IEnumerator{T}"/> to convert</param>
        /// <param name="runSynchronously">If True the enumeration will be performed on the same thread, otherwise the MoveNext will be executed on a separate thread with Task.Run method</param>
        /// <returns>Returns an instance of <see cref="IAsyncEnumerator{T}"/> implementation</returns>
        public static IAsyncEnumerator<T> ToAsyncEnumerator<T>(this IEnumerator<T> enumerator, bool runSynchronously = true)
        {
            if (enumerator == null)
                throw new ArgumentNullException(nameof(enumerator));
            return enumerator as IAsyncEnumerator<T> ?? new AsyncEnumeratorWrapper<T>(enumerator, runSynchronously);
        }
    }
}
