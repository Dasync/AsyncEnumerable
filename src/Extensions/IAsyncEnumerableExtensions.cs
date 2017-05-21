using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        #region Single / SingleOrDefault

        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/>.</param>
        public static Task<TSource> SingleAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken token = default(CancellationToken))
        {
            return SingleAsync(source, PredicateCache<TSource>.True, null, null, token);
        }

        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="noneExceptionMessage">The message of an exception which is thrown when the source collection is empty.</param>
        /// <param name="manyExceptionMessage">The message of an exception which is thrown when the source collection has more than one element.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/>.</param>
        public static Task<TSource> SingleAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            string noneExceptionMessage,
            string manyExceptionMessage,
            CancellationToken token = default(CancellationToken))
        {
            return SingleAsync(source, PredicateCache<TSource>.True, noneExceptionMessage, manyExceptionMessage, token);
        }

        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence that matches the criteria.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="predicate">Criteria predicate to select the only element.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/>.</param>
        public static Task<TSource> SingleAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken token = default(CancellationToken))
        {
            return SingleAsync(source, predicate, null, null, token);
        }

        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence that matches the criteria.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="predicate">Criteria predicate to select the only element.</param>
        /// <param name="noneExceptionMessage">The message of an exception which is thrown when the source collection is has no element matching the criteria.</param>
        /// <param name="manyExceptionMessage">The message of an exception which is thrown when the source collection has more than one element matching the criteria.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/>.</param>
        public static async Task<TSource> SingleAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            string noneExceptionMessage,
            string manyExceptionMessage,
            CancellationToken token = default(CancellationToken))
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            var matchFound = false;
            var lastMatch = default(TSource);

            using (var enumerator = await source.GetAsyncEnumeratorAsync(token).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(token).ConfigureAwait(false))
                {
                    if (predicate(enumerator.Current))
                    {
                        if (matchFound)
                            throw new InvalidOperationException(string.IsNullOrEmpty(manyExceptionMessage) ? "Several elements found matching the criteria." : manyExceptionMessage);

                        matchFound = true;
                        lastMatch = enumerator.Current;
                    }
                }
            }

            if (!matchFound)
                throw new InvalidOperationException(string.IsNullOrEmpty(noneExceptionMessage) ? "No element found matching the criteria." : noneExceptionMessage);

            return lastMatch;
        }

        /// <summary>
        /// Returns the only element of a sequence, and returns a default value if there is not exactly one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/>.</param>
        public static Task<TSource> SingleOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken token = default(CancellationToken))
        {
            return SingleOrDefaultAsync(source, PredicateCache<TSource>.True, token);
        }

        /// <summary>
        /// Returns the only element of a sequence, and returns a default value if there is not exactly one element in the sequence that matches the criteria.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="predicate">Criteria predicate to select the only element.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/>.</param>
        public static async Task<TSource> SingleOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken token = default(CancellationToken))
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            var matchFound = false;
            var lastMatch = default(TSource);

            using (var enumerator = await source.GetAsyncEnumeratorAsync(token).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(token).ConfigureAwait(false))
                {
                    if (predicate(enumerator.Current))
                    {
                        if (matchFound)
                        {
                            matchFound = false;
                            break;
                        }

                        matchFound = true;
                        lastMatch = enumerator.Current;
                    }
                }
            }

            if (!matchFound)
                return default(TSource);

            return lastMatch;
        }

        #endregion

        #region First / FirstOrDefault

        internal static class PredicateCache<T>
        {
            private static bool _true(T item) => true;
            public static readonly Func<T, bool> True = _true;
        }

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
            return FirstAsync(source, PredicateCache<TSource>.True, null, token);
        }

        /// <summary>
        /// Returns the first element in the <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="exceptionMessage">An optional custom exception message for the case when the <paramref name="source"/> is empty</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        public static Task<TSource> FirstAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            string exceptionMessage,
            CancellationToken token = default(CancellationToken))
        {
            return FirstAsync(source, PredicateCache<TSource>.True, exceptionMessage, token);
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        public static Task<TSource> FirstAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken token = default(CancellationToken))
        {
            return FirstAsync(source, predicate, null, token);
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="exceptionMessage">An optional custom exception message for the case when the <paramref name="source"/> is empty</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        public static async Task<TSource> FirstAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            string exceptionMessage,
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

            throw new InvalidOperationException(string.IsNullOrEmpty(exceptionMessage) ? "No Matching Element Found" : exceptionMessage);
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
            return FirstOrDefaultAsync(source, PredicateCache<TSource>.True, token);
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
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TResult> selector)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == selector)
                throw new ArgumentNullException(nameof(selector));

            return new AsyncEnumerableWithState<TResult, SelectContext<TSource, TResult>>(
                SelectContext<TSource, TResult>.Enumerate,
                new SelectContext<TSource, TResult> { Source = source, Selector = selector });
        }

        private struct SelectContext<TSource, TResult>
        {
            public IAsyncEnumerable<TSource> Source;
            public Func<TSource, TResult> Selector;

            private static async Task _enumerate(AsyncEnumerator<TResult>.Yield yield, SelectContext<TSource, TResult> context)
            {
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        await yield.ReturnAsync(context.Selector(enumerator.Current)).ConfigureAwait(false);
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<TResult>.Yield, SelectContext<TSource, TResult>, Task> Enumerate = _enumerate;
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element; the second parameter of the function represents the index of the source element.</param>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, long, TResult> selector)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == selector)
                throw new ArgumentNullException(nameof(selector));

            return new AsyncEnumerableWithState<TResult, SelectWithIndexContext<TSource, TResult>>(
                SelectWithIndexContext<TSource, TResult>.Enumerate,
                new SelectWithIndexContext<TSource, TResult> { Source = source, Selector = selector });
        }

        private struct SelectWithIndexContext<TSource, TResult>
        {
            public IAsyncEnumerable<TSource> Source;
            public Func<TSource, long, TResult> Selector;

            private static async Task _enumerate(AsyncEnumerator<TResult>.Yield yield, SelectWithIndexContext<TSource, TResult> context)
            {
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    long index = 0;
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        await yield.ReturnAsync(context.Selector(enumerator.Current, index)).ConfigureAwait(false);
                        index++;
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<TResult>.Yield, SelectWithIndexContext<TSource, TResult>, Task> Enumerate = _enumerate;
        }

        #endregion

        #region Take / TakeWhile

        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        public static IAsyncEnumerable<TSource> Take<TSource>(
            this IAsyncEnumerable<TSource> source,
            int count)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            if (count <= 0)
                return AsyncEnumerable<TSource>.Empty;

            return new AsyncEnumerableWithState<TSource, TakeContext<TSource>>(
                TakeContext<TSource>.Enumerate,
                new TakeContext<TSource> { Source = source, Count = count });
        }

        private struct TakeContext<TSource>
        {
            public IAsyncEnumerable<TSource> Source;
            public int Count;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, TakeContext<TSource> context)
            {
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    for (var i = context.Count; i > 0; i--)
                    {
                        if (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                            await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, TakeContext<TSource>, Task> Enumerate = _enumerate;
        }

        /// <summary>
        /// Returns elements from a sequence as long as a specified condition is true.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        public static IAsyncEnumerable<TSource> TakeWhile<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumerableWithState<TSource, TakeWhileContext<TSource>>(
                TakeWhileContext<TSource>.Enumerate,
                new TakeWhileContext<TSource> { Source = source, Predicate = predicate });
        }

        private struct TakeWhileContext<TSource>
        {
            public IAsyncEnumerable<TSource> Source;
            public Func<TSource, bool> Predicate;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, TakeWhileContext<TSource> context)
            {
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (context.Predicate(enumerator.Current))
                            await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                        else
                            break;
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, TakeWhileContext<TSource>, Task> Enumerate = _enumerate;
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
            var resultList = new List<T>();
            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    resultList.Add(enumerator.Current);
                }
            }
            return resultList.ToArray();
        }

        #endregion

        #region Skip / SkipWhile

        /// <summary>
        /// An <see cref="IAsyncEnumerable{T}"/> to return elements from.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        public static IAsyncEnumerable<TSource> Skip<TSource>(
            this IAsyncEnumerable<TSource> source,
            int count)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumerableWithState<TSource, SkipContext<TSource>>(
                SkipContext<TSource>.Enumerate,
                new SkipContext<TSource> { Source = source, Count = count });
        }

        private struct SkipContext<TSource>
        {
            public IAsyncEnumerable<TSource> Source;
            public int Count;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, SkipContext<TSource> context)
            {
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    var itemsToSkip = context.Count;
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (itemsToSkip > 0)
                            itemsToSkip--;
                        else
                            await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, SkipContext<TSource>, Task> Enumerate = _enumerate;
        }

        /// <summary>
        /// Bypasses elements in a sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        public static IAsyncEnumerable<TSource> SkipWhile<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumerableWithState<TSource, SkipWhileContext<TSource>>(
                SkipWhileContext<TSource>.Enumerate,
                new SkipWhileContext<TSource> { Source = source, Predicate = predicate });
        }

        private struct SkipWhileContext<TSource>
        {
            public IAsyncEnumerable<TSource> Source;
            public Func<TSource, bool> Predicate;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, SkipWhileContext<TSource> context)
            {
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    var yielding = false;
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (!yielding && !context.Predicate(enumerator.Current))
                            yielding = true;

                        if (yielding)
                            await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, SkipWhileContext<TSource>, Task> Enumerate = _enumerate;
        }

        #endregion

        #region Where

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        public static IAsyncEnumerable<TSource> Where<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumerableWithState<TSource, WhereContext<TSource>>(
                WhereContext<TSource>.Enumerate,
                new WhereContext<TSource> { Source = source, Predicate = predicate });
        }

        private struct WhereContext<TSource>
        {
            public IAsyncEnumerable<TSource> Source;
            public Func<TSource, bool> Predicate;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, WhereContext<TSource> context)
            {
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (context.Predicate(enumerator.Current))
                            await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, WhereContext<TSource>, Task> Enumerate = _enumerate;
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to filter.</param>
        /// <param name="predicate">A function to test each element for a condition; the second parameter of the function represents the index of the source element.</param>
        public static IAsyncEnumerable<TSource> Where<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, long, bool> predicate)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumerableWithState<TSource, WhereWithIndexContext<TSource>>(
                WhereWithIndexContext<TSource>.Enumerate,
                new WhereWithIndexContext<TSource> { Source = source, Predicate = predicate });
        }

        private struct WhereWithIndexContext<TSource>
        {
            public IAsyncEnumerable<TSource> Source;
            public Func<TSource, long, bool> Predicate;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, WhereWithIndexContext<TSource> context)
            {
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    long index = 0;
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (context.Predicate(enumerator.Current, index))
                            await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                        index++;
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, WhereWithIndexContext<TSource>, Task> Enumerate = _enumerate;
        }

        #endregion

        #region Cast

        /// <summary>
        /// Casts the elements of an <see cref="IAsyncEnumerable"/> to the specified type.
        /// </summary>
        /// <typeparam name="TResult">The type to cast the elements of <paramref name="source"/> to.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable"/> that contains the elements to be cast to type <typeparamref name="TResult"/>.</param>
        public static IAsyncEnumerable<TResult> Cast<TResult>(this IAsyncEnumerable source)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumerableWithState<TResult, CastContext<TResult>>(
                CastContext<TResult>.Enumerate,
                new CastContext<TResult> { Source = source });
        }

        private struct CastContext<TResult>
        {
            public IAsyncEnumerable Source;

            private static async Task _enumerate(AsyncEnumerator<TResult>.Yield yield, CastContext<TResult> context)
            {
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                        await yield.ReturnAsync((TResult)enumerator.Current).ConfigureAwait(false);
            }

            public static readonly Func<AsyncEnumerator<TResult>.Yield, CastContext<TResult>, Task> Enumerate = _enumerate;
        }

        #endregion

        #region DefaultIfEmpty

        /// <summary>
        /// Returns the elements of the specified sequence or the specified value in a singleton collection if the sequence is empty.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the specified value for if it is empty.</param>
        public static IAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this IAsyncEnumerable<TSource> source)
        {
            return DefaultIfEmpty(source, default(TSource));
        }

        /// <summary>
        /// Returns the elements of the specified sequence or the specified value in a singleton collection if the sequence is empty.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the specified value for if it is empty.</param>
        /// <param name="defaultValue">The value to return if the sequence is empty.</param>
        public static IAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this IAsyncEnumerable<TSource> source, TSource defaultValue)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumerableWithState<TSource, DefaultIfEmptyContext<TSource>>(
                DefaultIfEmptyContext<TSource>.Enumerate,
                new DefaultIfEmptyContext<TSource> { Source = source, DefaultValue = defaultValue });
        }

        private struct DefaultIfEmptyContext<TSource>
        {
            public IAsyncEnumerable<TSource> Source;
            public TSource DefaultValue;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, DefaultIfEmptyContext<TSource> context)
            {
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    var isEmpty = true;

                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        isEmpty = false;
                        await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                    }

                    if (isEmpty)
                        await yield.ReturnAsync(context.DefaultValue).ConfigureAwait(false);
                }
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, DefaultIfEmptyContext<TSource>, Task> Enumerate = _enumerate;
        }

        #endregion

        #region Batch

        /// <summary>
        /// Splits the input collection into series of batches.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to batch.</param>
        /// <param name="batchSize">The maximum number of elements to put in a batch.</param>
        public static IAsyncEnumerable<List<TSource>> Batch<TSource>(
            this IAsyncEnumerable<TSource> source,
            int batchSize)
        {
            return Batch<TSource, List<TSource>>(source, batchSize);
        }

        /// <summary>
        /// Splits the input collection into series of batches.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TStandardCollection">
        /// The type of a .NET's standard collection that forms a batch. Supported types are:
        /// <see cref="List{T}"/>, <see cref="Stack{T}"/>, <see cref="Queue{T}"/>, <see cref="HashSet{T}"/>,
        /// <see cref="LinkedList{T}"/>, <see cref="SortedSet{T}"/>, <see cref="BlockingCollection{T}"/>,
        /// <see cref="ConcurrentStack{T}"/>, <see cref="ConcurrentQueue{T}"/>, <see cref="ConcurrentBag{T}"/>.
        /// </typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to batch.</param>
        /// <param name="batchSize">The maximum number of elements to put in a batch.</param>
        public static IAsyncEnumerable<TStandardCollection> Batch<TSource, TStandardCollection>(
            this IAsyncEnumerable<TSource> source,
            int batchSize)
        {
            return Batch(source, batchSize, long.MaxValue, null,
                BatchCollectionHelper<TSource>.GetCreateCollectionFunction<TStandardCollection>(),
                BatchCollectionHelper<TSource>.GetAddToCollectionAction<TStandardCollection>());
        }

        /// <summary>
        /// Splits the input collection into series of batches.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to batch.</param>
        /// <param name="maxBatchWeight">The maximum logical weight of elements that a single batch can accomodate.</param>
        /// <param name="weightSelector">A function that computes a weight of a particular element, which is used to make a decision if it can fit into a batch.</param>
        public static IAsyncEnumerable<List<TSource>> Batch<TSource>(
            this IAsyncEnumerable<TSource> source,
            long maxBatchWeight,
            Func<TSource, long> weightSelector)
        {
            return Batch<TSource, List<TSource>>(source, maxBatchWeight, weightSelector);
        }

        /// <summary>
        /// Splits the input collection into series of batches.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TStandardCollection">
        /// The type of a .NET's standard collection that forms a batch. Supported types are:
        /// <see cref="List{T}"/>, <see cref="Stack{T}"/>, <see cref="Queue{T}"/>, <see cref="HashSet{T}"/>,
        /// <see cref="LinkedList{T}"/>, <see cref="SortedSet{T}"/>, <see cref="BlockingCollection{T}"/>,
        /// <see cref="ConcurrentStack{T}"/>, <see cref="ConcurrentQueue{T}"/>, <see cref="ConcurrentBag{T}"/>.
        /// </typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to batch.</param>
        /// <param name="maxBatchWeight">The maximum logical weight of elements that a single batch can accomodate.</param>
        /// <param name="weightSelector">A function that computes a weight of a particular element, which is used to make a decision if it can fit into a batch.</param>
        public static IAsyncEnumerable<TStandardCollection> Batch<TSource, TStandardCollection>(
            this IAsyncEnumerable<TSource> source,
            long maxBatchWeight,
            Func<TSource, long> weightSelector)
        {
            return Batch(source, null, maxBatchWeight, weightSelector,
                BatchCollectionHelper<TSource>.GetCreateCollectionFunction<TStandardCollection>(),
                BatchCollectionHelper<TSource>.GetAddToCollectionAction<TStandardCollection>());
        }

        /// <summary>
        /// Splits the input collection into series of batches.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to batch.</param>
        /// <param name="maxItemsInBatch">The maximum number of elements to put in a batch regardless their total weight.</param>
        /// <param name="maxBatchWeight">The maximum logical weight of elements that a single batch can accomodate.</param>
        /// <param name="weightSelector">A function that computes a weight of a particular element, which is used to make a decision if it can fit into a batch.</param>
        public static IAsyncEnumerable<List<TSource>> Batch<TSource>(
            this IAsyncEnumerable<TSource> source,
            int maxItemsInBatch,
            long maxBatchWeight,
            Func<TSource, long> weightSelector)
        {
            return Batch<TSource, List<TSource>>(source, maxItemsInBatch, maxBatchWeight, weightSelector);
        }

        /// <summary>
        /// Splits the input collection into series of batches.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TStandardCollection">
        /// The type of a .NET's standard collection that forms a batch. Supported types are:
        /// <see cref="List{T}"/>, <see cref="Stack{T}"/>, <see cref="Queue{T}"/>, <see cref="HashSet{T}"/>,
        /// <see cref="LinkedList{T}"/>, <see cref="SortedSet{T}"/>, <see cref="BlockingCollection{T}"/>,
        /// <see cref="ConcurrentStack{T}"/>, <see cref="ConcurrentQueue{T}"/>, <see cref="ConcurrentBag{T}"/>.
        /// </typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to batch.</param>
        /// <param name="maxItemsInBatch">The maximum number of elements to put in a batch regardless their total weight.</param>
        /// <param name="maxBatchWeight">The maximum logical weight of elements that a single batch can accomodate.</param>
        /// <param name="weightSelector">A function that computes a weight of a particular element, which is used to make a decision if it can fit into a batch.</param>
        public static IAsyncEnumerable<TStandardCollection> Batch<TSource, TStandardCollection>(
            this IAsyncEnumerable<TSource> source,
            int maxItemsInBatch,
            long maxBatchWeight,
            Func<TSource, long> weightSelector)
        {
            return Batch(source, maxItemsInBatch, maxBatchWeight, weightSelector,
                BatchCollectionHelper<TSource>.GetCreateCollectionFunction<TStandardCollection>(),
                BatchCollectionHelper<TSource>.GetAddToCollectionAction<TStandardCollection>());
        }

        /// <summary>
        /// Splits the input collection into series of batches.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TBatch">The type of a batch of elements.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to batch.</param>
        /// <param name="maxItemsInBatch">The maximum number of elements to put in a batch regardless their total weight.</param>
        /// <param name="maxBatchWeight">The maximum logical weight of elements that a single batch can accomodate.</param>
        /// <param name="weightSelector">A function that computes a weight of a particular element, which is used to make a decision if it can fit into a batch.</param>
        /// <param name="createBatch">A function that creates a new batch with optional suggested capacity.</param>
        /// <param name="addItem">An action that adds an element to a batch.</param>
        public static IAsyncEnumerable<TBatch> Batch<TSource, TBatch>(
            this IAsyncEnumerable<TSource> source,
            int? maxItemsInBatch,
            long maxBatchWeight,
            Func<TSource, long> weightSelector,
            Func<int?, TBatch> createBatch,
            Action<TBatch, TSource> addItem)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source), "You must supply a source collection.");
            if (maxItemsInBatch <= 0)
                throw new ArgumentException("Batch size must be a positive number.", nameof(maxItemsInBatch));
            if (createBatch == null)
                throw new ArgumentNullException(nameof(createBatch), "You must specify a function that creates a batch.");
            if (addItem == null)
                throw new ArgumentNullException(nameof(addItem), "You must specify an action that adds an item to a batch.");

            if (maxItemsInBatch == null || weightSelector == null)
                throw new InvalidOperationException("You must supply either a max batch size or a weight selector.");

            return new AsyncEnumerableWithState<TBatch, BatchContext<TSource, TBatch>>(
                BatchContext<TSource, TBatch>.Enumerate,
                new BatchContext<TSource, TBatch>
                {
                    CreateBatch = createBatch,
                    AddItemToBatch = addItem,
                    BatchPreallocateSize = maxItemsInBatch,
                    MaxItemsInBatch = maxItemsInBatch ?? int.MaxValue,
                    MaxBatchWeight = maxBatchWeight,
                    WeightSelector = weightSelector,
                    Source = source
                });
        }

        internal static class BatchCollectionHelper<TSource>
        {
            private static List<TSource> CreateList(int? capacity) => capacity.HasValue ? new List<TSource>(capacity.Value) : new List<TSource>();
            private static void AddToList(List<TSource> list, TSource item) => list.Add(item);
            private static readonly Func<int?, List<TSource>> _createList = CreateList;
            private static readonly Action<List<TSource>, TSource> _addToList = AddToList;

            private static Stack<TSource> CreateStack(int? capacity) => capacity.HasValue ? new Stack<TSource>(capacity.Value) : new Stack<TSource>();
            private static void AddToStack(Stack<TSource> stack, TSource item) => stack.Push(item);
            private static readonly Func<int?, Stack<TSource>> _createStack = CreateStack;
            private static readonly Action<Stack<TSource>, TSource> _addToStack = AddToStack;

            private static Queue<TSource> CreateQueue(int? capacity) => capacity.HasValue ? new Queue<TSource>(capacity.Value) : new Queue<TSource>();
            private static void AddToQueue(Queue<TSource> queue, TSource item) => queue.Enqueue(item);
            private static readonly Func<int?, Queue<TSource>> _createQueue = CreateQueue;
            private static readonly Action<Queue<TSource>, TSource> _addToQueue = AddToQueue;

            private static HashSet<TSource> CreateHashSet(int? capacity) => new HashSet<TSource>();
            private static void AddToHashSet(HashSet<TSource> hashSet, TSource item) => hashSet.Add(item);
            private static readonly Func<int?, HashSet<TSource>> _createHashSet = CreateHashSet;
            private static readonly Action<HashSet<TSource>, TSource> _addToHashSet = AddToHashSet;

            private static LinkedList<TSource> CreateLinkedList(int? capacity) => new LinkedList<TSource>();
            private static void AddToLinkedList(LinkedList<TSource> linkedList, TSource item) => linkedList.AddLast(item);
            private static readonly Func<int?, LinkedList<TSource>> _createLinkedList = CreateLinkedList;
            private static readonly Action<LinkedList<TSource>, TSource> _addToLinkedList = AddToLinkedList;

            private static SortedSet<TSource> CreateSortedSet(int? capacity) => new SortedSet<TSource>();
            private static void AddToSortedSet(SortedSet<TSource> sortedSet, TSource item) => sortedSet.Add(item);
            private static readonly Func<int?, SortedSet<TSource>> _createSortedSet = CreateSortedSet;
            private static readonly Action<SortedSet<TSource>, TSource> _addToSortedSet = AddToSortedSet;

            private static BlockingCollection<TSource> CreateBlockingCollection(int? capacity) => capacity.HasValue ? new BlockingCollection<TSource>(capacity.Value) : new BlockingCollection<TSource>();
            private static void AddToBlockingCollection(BlockingCollection<TSource> blockingCollection, TSource item) => blockingCollection.Add(item);
            private static readonly Func<int?, BlockingCollection<TSource>> _createBlockingCollection = CreateBlockingCollection;
            private static readonly Action<BlockingCollection<TSource>, TSource> _addToBlockingCollection = AddToBlockingCollection;

            private static ConcurrentStack<TSource> CreateConcurrentStack(int? capacity) => new ConcurrentStack<TSource>();
            private static void AddToConcurrentStack(ConcurrentStack<TSource> concurrentStack, TSource item) => concurrentStack.Push(item);
            private static readonly Func<int?, ConcurrentStack<TSource>> _createConcurrentStack = CreateConcurrentStack;
            private static readonly Action<ConcurrentStack<TSource>, TSource> _addToConcurrentStack = AddToConcurrentStack;

            private static ConcurrentQueue<TSource> CreateConcurrentQueue(int? capacity) => new ConcurrentQueue<TSource>();
            private static void AddToConcurrentQueue(ConcurrentQueue<TSource> concurrentQueue, TSource item) => concurrentQueue.Enqueue(item);
            private static readonly Func<int?, ConcurrentQueue<TSource>> _createConcurrentQueue = CreateConcurrentQueue;
            private static readonly Action<ConcurrentQueue<TSource>, TSource> _addToConcurrentQueue = AddToConcurrentQueue;

            private static ConcurrentBag<TSource> CreateConcurrentBag(int? capacity) => new ConcurrentBag<TSource>();
            private static void AddToConcurrentBag(ConcurrentBag<TSource> concurrentBag, TSource item) => concurrentBag.Add(item);
            private static readonly Func<int?, ConcurrentBag<TSource>> _createConcurrentBag = CreateConcurrentBag;
            private static readonly Action<ConcurrentBag<TSource>, TSource> _addToConcurrentBag = AddToConcurrentBag;

            public static Func<int?, TCollection> GetCreateCollectionFunction<TCollection>()
            {
                if (typeof(TCollection) == typeof(List<TSource>))
                    return (Func<int?, TCollection>)(object)_createList;

                if (typeof(TCollection) == typeof(Stack<TSource>))
                    return (Func<int?, TCollection>)(object)_createStack;

                if (typeof(TCollection) == typeof(Queue<TSource>))
                    return (Func<int?, TCollection>)(object)_createQueue;

                if (typeof(TCollection) == typeof(HashSet<TSource>))
                    return (Func<int?, TCollection>)(object)_createHashSet;

                if (typeof(TCollection) == typeof(LinkedList<TSource>))
                    return (Func<int?, TCollection>)(object)_createLinkedList;

                if (typeof(TCollection) == typeof(SortedSet<TSource>))
                    return (Func<int?, TCollection>)(object)_createSortedSet;

                if (typeof(TCollection) == typeof(BlockingCollection<TSource>))
                    return (Func<int?, TCollection>)(object)_createBlockingCollection;

                if (typeof(TCollection) == typeof(ConcurrentStack<TSource>))
                    return (Func<int?, TCollection>)(object)_createConcurrentStack;

                if (typeof(TCollection) == typeof(ConcurrentQueue<TSource>))
                    return (Func<int?, TCollection>)(object)_createConcurrentQueue;

                if (typeof(TCollection) == typeof(ConcurrentBag<TSource>))
                    return (Func<int?, TCollection>)(object)_createConcurrentBag;

                throw new NotSupportedException($"The collection of type '{typeof(TCollection).FullName}' is not supported.");
            }

            public static Action<TCollection, TSource> GetAddToCollectionAction<TCollection>()
            {
                if (typeof(TCollection) == typeof(List<TSource>))
                    return (Action<TCollection, TSource>)(object)_addToList;

                if (typeof(TCollection) == typeof(Stack<TSource>))
                    return (Action<TCollection, TSource>)(object)_addToStack;

                if (typeof(TCollection) == typeof(Queue<TSource>))
                    return (Action<TCollection, TSource>)(object)_addToQueue;

                if (typeof(TCollection) == typeof(HashSet<TSource>))
                    return (Action<TCollection, TSource>)(object)_addToHashSet;

                if (typeof(TCollection) == typeof(LinkedList<TSource>))
                    return (Action<TCollection, TSource>)(object)_addToLinkedList;

                if (typeof(TCollection) == typeof(SortedSet<TSource>))
                    return (Action<TCollection, TSource>)(object)_addToSortedSet;

                if (typeof(TCollection) == typeof(BlockingCollection<TSource>))
                    return (Action<TCollection, TSource>)(object)_addToBlockingCollection;

                if (typeof(TCollection) == typeof(ConcurrentStack<TSource>))
                    return (Action<TCollection, TSource>)(object)_addToConcurrentStack;

                if (typeof(TCollection) == typeof(ConcurrentQueue<TSource>))
                    return (Action<TCollection, TSource>)(object)_addToConcurrentQueue;

                if (typeof(TCollection) == typeof(ConcurrentBag<TSource>))
                    return (Action<TCollection, TSource>)(object)_addToConcurrentBag;

                throw new NotSupportedException($"The collection of type '{typeof(TCollection).FullName}' is not supported.");
            }
        }

        private struct BatchContext<TSource, TBatch>
        {
            public IAsyncEnumerable<TSource> Source;
            public int? BatchPreallocateSize;
            public int MaxItemsInBatch;
            public long MaxBatchWeight;
            public Func<TSource, long> WeightSelector;
            public Func<int?, TBatch> CreateBatch;
            public Action<TBatch, TSource> AddItemToBatch;

            private static async Task _enumerate(AsyncEnumerator<TBatch>.Yield yield, BatchContext<TSource, TBatch> context)
            {
                var batch = default(TBatch);
                var itemsInBatch = 0;
                var batchWeight = 0L;

                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        var itemWeight = context.WeightSelector?.Invoke(enumerator.Current) ?? 0L;

                        // Check if item does not fit into existing batch.
                        if (itemsInBatch > 0 && batchWeight + itemWeight > context.MaxBatchWeight)
                        {
                            await yield.ReturnAsync(batch).ConfigureAwait(false);
                            batch = default(TBatch);
                            itemsInBatch = 0;
                            batchWeight = 0;
                        }

                        if (itemsInBatch == 0)
                            batch = context.CreateBatch(context.BatchPreallocateSize);

                        context.AddItemToBatch(batch, enumerator.Current);
                        batchWeight += itemWeight;

                        if (itemsInBatch >= context.MaxItemsInBatch || batchWeight >= context.MaxBatchWeight)
                        {
                            await yield.ReturnAsync(batch).ConfigureAwait(false);
                            batch = default(TBatch);
                            itemsInBatch = 0;
                            batchWeight = 0;
                        }
                    }

                    if (itemsInBatch > 0)
                        await yield.ReturnAsync(batch).ConfigureAwait(false);
                }
            }

            public static readonly Func<AsyncEnumerator<TBatch>.Yield, BatchContext<TSource, TBatch>, Task> Enumerate = _enumerate;
        }

        #endregion
    }
}
