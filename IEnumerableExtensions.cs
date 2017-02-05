using System.Collections.Async;
using System.Linq;

namespace System.Collections
{
    /// <summary>
    /// Converts generic IEnumerable to IAsyncEnumerable
    /// </summary>
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Converts <see cref="IEnumerable"/> to <see cref="IAsyncEnumerable"/>
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
        /// Converts generic <see cref="IEnumerable{T}"/> to <see cref="IAsyncEnumerable{T}"/>
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
        /// Converts generic <see cref="IEnumerator{T}"/> to <see cref="IAsyncEnumerator{T}"/>
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
