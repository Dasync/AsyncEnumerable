using System.Collections.Async;
using System.Collections.Async.Internals;
using System.Collections.Generic;
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
            if (ReferenceEquals(enumerable, Enumerable.Empty<T>()))
                return AsyncEnumerable<T>.Empty;
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

            if (enumerable is IAsyncEnumerable<T> asyncEnumerable)
                return asyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken.None).GetAwaiter().GetResult();

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

namespace System.Collections.Async
{
    /// <summary>
    /// Extension methods for <see cref="IAsyncEnumerable{T}"/> for backward compatibility with version 1 of this libraray.
    /// Not recommended to use.
    /// </summary>
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    public static class AsyncEnumerableAdapterExtensions
    {
        /// <summary>
        /// Converts <see cref="IAsyncEnumerable"/> to <see cref="IEnumerable"/>.
        /// This method is marked as [Obsolete] to discourage you from doing such conversion,
        /// which defeats the whole purpose of having a non-blocking async enumeration,
        /// and what might lead to dead-locks in ASP.NET or WPF applications.
        /// </summary>
        [Obsolete]
        public static IEnumerable ToEnumerable(this IAsyncEnumerable asyncEnumerable)
        {
            if (asyncEnumerable == null)
                throw new ArgumentNullException(nameof(asyncEnumerable));
            if (asyncEnumerable is IEnumerable enumerable)
                return enumerable;
            return new EnumerableAdapter(asyncEnumerable);
        }

        /// <summary>
        /// Converts <see cref="IAsyncEnumerable{T}"/> to <see cref="IEnumerable{T}"/>.
        /// This method is marked as [Obsolete] to discourage you from doing such conversion,
        /// which defeats the whole purpose of having a non-blocking async enumeration,
        /// and what might lead to dead-locks in ASP.NET or WPF applications.
        /// </summary>
        [Obsolete]
        public static IEnumerable<T> ToEnumerable<T>(this IAsyncEnumerable<T> asyncEnumerable)
        {
            if (asyncEnumerable == null)
                throw new ArgumentNullException(nameof(asyncEnumerable));
            if (asyncEnumerable is IEnumerable<T> enumerable)
                return enumerable;
            return new EnumerableAdapter<T>(asyncEnumerable);
        }

        /// <summary>
        /// Converts <see cref="IAsyncEnumerator"/> to <see cref="IEnumerator"/>.
        /// This method is marked as [Obsolete] to discourage you from doing such conversion,
        /// which defeats the whole purpose of having a non-blocking async enumeration,
        /// and what might lead to dead-locks in ASP.NET or WPF applications.
        /// </summary>
        [Obsolete]
        public static IEnumerator ToEnumerator(this IAsyncEnumerator asyncEnumerator)
        {
            if (asyncEnumerator == null)
                throw new ArgumentNullException(nameof(asyncEnumerator));
            if (asyncEnumerator is IEnumerator enumerator)
                return enumerator;
            return new EnumeratorAdapter(asyncEnumerator);
        }

        /// <summary>
        /// Converts <see cref="IAsyncEnumerator{T}"/> to <see cref="IEnumerator{T}"/>.
        /// This method is marked as [Obsolete] to discourage you from doing such conversion,
        /// which defeats the whole purpose of having a non-blocking async enumeration,
        /// and what might lead to dead-locks in ASP.NET or WPF applications.
        /// </summary>
        [Obsolete]
        public static IEnumerator<T> ToEnumerator<T>(this IAsyncEnumerator<T> asyncEnumerator)
        {
            if (asyncEnumerator == null)
                throw new ArgumentNullException(nameof(asyncEnumerator));
            if (asyncEnumerator is IEnumerator<T> enumerator)
                return enumerator;
            return new EnumeratorAdapter<T>(asyncEnumerator);
        }

        /// <summary>
        /// Creates an enumerator that iterates through a collection synchronously.
        /// This method is marked as [Obsolete] to discourage you from using this synchronous version of
        /// the method instead of <see cref="IAsyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken)"/>,
        /// what might lead to dead-locks in ASP.NET or WPF applications.
        /// </summary>
        [Obsolete]
        public static IEnumerator GetEnumerator(this IAsyncEnumerable asyncEnumerable)
        {
            if (asyncEnumerable == null)
                throw new ArgumentNullException(nameof(asyncEnumerable));
            return asyncEnumerable.GetAsyncEnumeratorAsync().GetAwaiter().GetResult().ToEnumerator();
        }

        /// <summary>
        /// Creates an enumerator that iterates through a collection synchronously.
        /// This method is marked as [Obsolete] to discourage you from using this synchronous version of
        /// the method instead of <see cref="IAsyncEnumerable{T}.GetAsyncEnumeratorAsync(CancellationToken)"/>,
        /// what might lead to dead-locks in ASP.NET or WPF applications.
        /// </summary>
        [Obsolete]
        public static IEnumerator<T> GetEnumerator<T>(this IAsyncEnumerable<T> asyncEnumerable)
        {
            if (asyncEnumerable == null)
                throw new ArgumentNullException(nameof(asyncEnumerable));
            return asyncEnumerable.GetAsyncEnumeratorAsync().GetAwaiter().GetResult().ToEnumerator();
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection synchronously.
        /// This method is marked as [Obsolete] to discourage you from using this synchronous version of
        /// the method instead of <see cref="IAsyncEnumerator.MoveNextAsync(CancellationToken)"/>,
        /// what might lead to dead-locks in ASP.NET or WPF applications.
        /// </summary>
        [Obsolete]
        public static bool MoveNext(this IAsyncEnumerator asyncEnumerator)
        {
            if (asyncEnumerator == null)
                throw new ArgumentNullException(nameof(asyncEnumerator));
            return asyncEnumerator.MoveNextAsync().GetAwaiter().GetResult();
        }
    }
}