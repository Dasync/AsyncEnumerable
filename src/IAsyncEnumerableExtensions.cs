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
            return FirstAsync(source, PredicateCache<TSource>.True, token);
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
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="IAsyncEnumerator.ResetAsync(CancellationToken)"/> method is not allowed</param>
        public static IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TResult> selector,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == selector)
                throw new ArgumentNullException(nameof(selector));

            return new AsyncEnumerableWithState<TResult, SelectContext<TSource, TResult>>(
                SelectContext<TSource, TResult>.Enumerate,
                new SelectContext<TSource, TResult> { Source = source, Selector = selector },
                oneTimeUse);
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
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="IAsyncEnumerator.ResetAsync(CancellationToken)"/> method is not allowed</param>
        public static IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, long, TResult> selector,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == selector)
                throw new ArgumentNullException(nameof(selector));

            return new AsyncEnumerableWithState<TResult, SelectWithIndexContext<TSource, TResult>>(
                SelectWithIndexContext<TSource, TResult>.Enumerate,
                new SelectWithIndexContext<TSource, TResult> { Source = source, Selector = selector },
                oneTimeUse);
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
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="IAsyncEnumerator.ResetAsync(CancellationToken)"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> TakeAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            int count,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            if (count <= 0)
                return AsyncEnumerable<TSource>.Empty;

            return new AsyncEnumerableWithState<TSource, TakeContext<TSource>>(
                TakeContext<TSource>.Enumerate,
                new TakeContext<TSource> { Source = source, Count = count },
                oneTimeUse);
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
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="IAsyncEnumerator.ResetAsync(CancellationToken)"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> TakeWhileAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumerableWithState<TSource, TakeWhileContext<TSource>>(
                TakeWhileContext<TSource>.Enumerate,
                new TakeWhileContext<TSource> { Source = source, Predicate = predicate },
                oneTimeUse);
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
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="IAsyncEnumerator.ResetAsync(CancellationToken)"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> SkipAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            int count,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumerableWithState<TSource, SkipContext<TSource>>(
                SkipContext<TSource>.Enumerate,
                new SkipContext<TSource> { Source = source, Count = count },
                oneTimeUse);
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
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="IAsyncEnumerator.ResetAsync(CancellationToken)"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> SkipWhileAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumerableWithState<TSource, SkipWhileContext<TSource>>(
                SkipWhileContext<TSource>.Enumerate,
                new SkipWhileContext<TSource> { Source = source, Predicate = predicate },
                oneTimeUse);
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
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="IAsyncEnumerator.ResetAsync(CancellationToken)"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> WhereAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumerableWithState<TSource, WhereContext<TSource>>(
                WhereContext<TSource>.Enumerate,
                new WhereContext<TSource> { Source = source, Predicate = predicate },
                oneTimeUse);
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
        /// <param name="oneTimeUse">When <c>true</c> the enumeration can be performed once only and <see cref="IAsyncEnumerator.ResetAsync(CancellationToken)"/> method is not allowed</param>
        public static IAsyncEnumerable<TSource> WhereAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, long, bool> predicate,
            bool oneTimeUse = false)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumerableWithState<TSource, WhereWithIndexContext<TSource>>(
                WhereWithIndexContext<TSource>.Enumerate,
                new WhereWithIndexContext<TSource> { Source = source, Predicate = predicate },
                oneTimeUse);
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
    }
}
