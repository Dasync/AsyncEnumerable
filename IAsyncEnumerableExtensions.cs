using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    /// <summary>
    /// Extension methods for <see cref="IAsyncEnumerable{T}"/> interface
    /// </summary>
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    public static class IAsyncEnumerableExtensions
    {
        #region First / FirstOrDefault

        /// <summary>
        /// Returns the first element in the <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        public static Task<TSource> FirstAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken token = default(CancellationToken))
        {
            return FirstAsync(source, _ => true, token);
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        public static async Task<TSource> FirstAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken token = default(CancellationToken))
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            using (var enumerator = await source.GetAsyncEnumeratorAsync(token).ConfigureAwait(false))
                while (await enumerator.MoveNextAsync(token).ConfigureAwait(false))
                    if (predicate(enumerator.Current))
                        return enumerator.Current;

            throw new InvalidOperationException("No Matching Element Found");
        }

        /// <summary>
        /// Returns the first element in the <see cref="IAsyncEnumerable{T}"/>, or a default value if no element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        public static Task<TSource> FirstOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken token = default(CancellationToken))
        {
            return FirstOrDefaultAsync(source, _ => true, token);
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition, or a default value if no element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        public static async Task<TSource> FirstOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken token = default(CancellationToken))
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            using (var enumerator = await source.GetAsyncEnumeratorAsync(token).ConfigureAwait(false))
                while (await enumerator.MoveNextAsync(token).ConfigureAwait(false))
                    if (predicate(enumerator.Current))
                        return enumerator.Current;

            return default(TSource);
        }

        #endregion

        #region Select

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="AsyncEnumerator{T}.Reset"/> method is not allowed</param>
        public static IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TResult> selector,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == selector)
                throw new ArgumentNullException(nameof(selector));

            return new AsyncEnumerable<TResult>(
                yield =>
                    source.ForEachAsync(
                        item => yield.ReturnAsync(selector(item)),
                        yield.CancellationToken),
                oneTimeUse);
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element; the second parameter of the function represents the index of the source element.</param>
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="AsyncEnumerator{T}.Reset"/> method is not allowed</param>
        public static IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, long, TResult> selector,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == selector)
                throw new ArgumentNullException(nameof(selector));

            return new AsyncEnumerable<TResult>(
                yield =>
                    source.ForEachAsync(
                        (item, index) => yield.ReturnAsync(selector(item, index)),
                        yield.CancellationToken),
                oneTimeUse);
        }

        #endregion

        #region Take / TakeWhile

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="AsyncEnumerator{T}.Reset"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> TakeAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            int count,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            if (count <= 0)
                return AsyncEnumerable<TSource>.Empty;

            return new AsyncEnumerable<TSource>(
                async yield =>
                {
                    using (var enumerator = await source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                        while (count > 0)
                        {
                            if (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                                await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);

                            count--;
                        }
                },
                oneTimeUse);
        }

        /// <summary>
        /// Returns elements from a sequence as long as a specified condition is true.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="AsyncEnumerator{T}.Reset"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> TakeWhileAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumerable<TSource>(
                async yield =>
                {
                    using (var enumerator = await source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                        while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                            if (predicate(enumerator.Current))
                                await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                            else
                                break;
                },
                oneTimeUse);
        }

        #endregion

        #region ToList
        /// <summary>
        /// Creates a list of elements asynchronously from the enumerable source
        /// </summary>
        /// <typeparam name="T">The type of the elements of source</typeparam>
        /// <param name="source">The collection of elements</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation</param>
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            var resultList = new List<T>();
            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    resultList.Add(enumerator.Current);
                }
            }
            return resultList;
        }
        #endregion

        #region ToArray
        /// <summary>
        /// Creates an array of elements asynchronously from the enumerable source
        /// </summary>
        /// <typeparam name="T">The type of the elements of source</typeparam>
        /// <param name="source">The collection of elements</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation</param>
        public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            var list = await source.ToListAsync(cancellationToken).ConfigureAwait(false);
            return list.ToArray();
        }
        #endregion

        #region Skip / SkipWhile

        /// <summary>
        /// An <see cref="IAsyncEnumerable{T}"/> to return elements from.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="AsyncEnumerator{T}.Reset"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> SkipAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            int count,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumerable<TSource>(
                async yield =>
                {
                    using (var enumerator = await source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                        while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                            if (count-- <= 0)
                                await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                },
                oneTimeUse);
        }

        /// <summary>
        /// Bypasses elements in a sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="AsyncEnumerator{T}.Reset"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> SkipWhileAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumerable<TSource>(
                async yield =>
                {
                    var yielding = false;

                    using (var enumerator = await source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (!yielding && !predicate(enumerator.Current))
                            yielding = true;

                        if (yielding)
                            await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                    }
                },
                oneTimeUse);
        }

        #endregion

        #region Where

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="AsyncEnumerator{T}.Reset"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> WhereAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumerable<TSource>(
                async yield =>
                {
                    using (var enumerator = await source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (predicate(enumerator.Current))
                            await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                    }                            
                },                
                oneTimeUse);
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to filter.</param>
        /// <param name="predicate">A function to test each element for a condition; the second parameter of the function represents the index of the source element.</param>
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="AsyncEnumerator{T}.Reset"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> WhereAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, long, bool> predicate,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            long index = 0;

            return new AsyncEnumerable<TSource>(
                async yield =>
                {
                    using (var enumerator = await source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (predicate(enumerator.Current, index))
                            await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);

                        index++;
                    }
                },
                oneTimeUse);
        }
        #endregion
    }
}
