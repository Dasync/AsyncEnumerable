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
        /// Converts IEnumerable to IAsyncEnumerable 
        /// </summary>
        /// <param name="enumerable">The instance of IEnumerable to convert</param>
        /// <param name="runSynchronously">If True the enumeration will be performed on the same thread, otherwise the MoveNext will be executed on a separate thread with Task.Run method</param>
        /// <returns>Returns an instance of IAsyncEnumerable implementation</returns>
        public static IAsyncEnumerable ToAsyncEnumerable(this IEnumerable enumerable, bool runSynchronously = false)
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
        /// Converts generic IEnumerable to IAsyncEnumerable 
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <param name="enumerable">The instance of IEnumerable to convert</param>
        /// <param name="runSynchronously">If True the enumeration will be performed on the same thread, otherwise the MoveNext will be executed on a separate thread with Task.Run method</param>
        /// <returns>Returns an instance of IAsyncEnumerable implementation</returns>
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable, bool runSynchronously = false)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            return enumerable as IAsyncEnumerable<T> ?? new AsyncEnumerableWrapper<T>(enumerable, runSynchronously);
        }
    }
}
