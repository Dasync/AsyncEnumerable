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
            CancellationToken token = default)
            => SingleAsync(source, PredicateCache<TSource>.True, null, null, token);

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
            CancellationToken token = default)
            => SingleAsync(source, PredicateCache<TSource>.True, noneExceptionMessage, manyExceptionMessage, token);

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
            CancellationToken token = default)
            => SingleAsync(source, predicate, null, null, token);

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
            CancellationToken token = default)
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
            CancellationToken token = default)
            => SingleOrDefaultAsync(source, PredicateCache<TSource>.True, token);

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
            CancellationToken token = default)
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
                return default;

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
            CancellationToken token = default)
            => FirstAsync(source, PredicateCache<TSource>.True, null, token);

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
            CancellationToken token = default)
            => FirstAsync(source, PredicateCache<TSource>.True, exceptionMessage, token);

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
            CancellationToken token = default)
            => FirstAsync(source, predicate, null, token);

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
            CancellationToken token = default)
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
            CancellationToken token = default)
            => FirstOrDefaultAsync(source, PredicateCache<TSource>.True, token);

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
            CancellationToken token = default)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            using (var enumerator = await source.GetAsyncEnumeratorAsync(token).ConfigureAwait(false))
                while (await enumerator.MoveNextAsync(token).ConfigureAwait(false))
                    if (predicate(enumerator.Current))
                        return enumerator.Current;

            return default;
        }

        #endregion

        #region Select

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
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
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
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

        #region SelectMany

        /// <summary>
        /// Projects each element of a sequence to an IAsyncEnumerable&lt;T&gt; and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the value in the IAsyncEnumerable returned by <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IAsyncEnumerable<TResult>> selector)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == selector)
                throw new ArgumentNullException(nameof(selector));

            return new AsyncEnumerableWithState<TResult, SelectManyContext<TSource, TResult, TResult>>(
                SelectManyContext<TSource, TResult, TResult>.Enumerate,
                new SelectManyContext<TSource, TResult, TResult>
                {
                    Source = source,
                    CollectionSelector = selector,
                    ResultSelector = ZeroTransformHelper<TSource, TResult, TResult>.ReturnItem
                });
        }

        /// <summary>
        /// Projects each element of a sequence to an IAsyncEnumerable&lt;T&gt; and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TItem">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TItem, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IAsyncEnumerable<TItem>> collectionSelector,
            Func<TSource, TItem, TResult> resultSelector)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == collectionSelector)
                throw new ArgumentNullException(nameof(collectionSelector));
            if (null == resultSelector)
                throw new ArgumentNullException(nameof(resultSelector));

            return new AsyncEnumerableWithState<TResult, SelectManyContext<TSource, TItem, TResult>>(
                SelectManyContext<TSource, TItem, TResult>.Enumerate,
                new SelectManyContext<TSource, TItem, TResult>
                {
                    Source = source,
                    CollectionSelector = collectionSelector,
                    ResultSelector = resultSelector
                });
        }

        private struct SelectManyContext<TSource, TItem, TResult>
        {
            public IAsyncEnumerable<TSource> Source;
            public Func<TSource, IAsyncEnumerable<TItem>> CollectionSelector;
            public Func<TSource, TItem, TResult> ResultSelector;

            private static async Task _enumerate(AsyncEnumerator<TResult>.Yield yield, SelectManyContext<TSource, TItem, TResult> context)
            {
                using (IAsyncEnumerator<TSource> sourceEnumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    while (await sourceEnumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        var items = context.CollectionSelector(sourceEnumerator.Current);

                        using (var itemsEnumerator = await items.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                        {
                            while (await itemsEnumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                            {
                                var resultItem = context.ResultSelector(sourceEnumerator.Current, itemsEnumerator.Current);
                                await yield.ReturnAsync(resultItem).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<TResult>.Yield, SelectManyContext<TSource, TItem, TResult>, Task> Enumerate = _enumerate;
        }

        /// <summary>
        /// Projects each element of a sequence to an IAsyncEnumerable&lt;T&gt; and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the value in the IAsyncEnumerable returned by <paramref name="selector"/>.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element.</param>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IEnumerable<TResult>> selector)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == selector)
                throw new ArgumentNullException(nameof(selector));

            return new AsyncEnumerableWithState<TResult, SelectManySyncContext<TSource, TResult, TResult>>(
                SelectManySyncContext<TSource, TResult, TResult>.Enumerate,
                new SelectManySyncContext<TSource, TResult, TResult>
                {
                    Source = source,
                    CollectionSelector = selector,
                    ResultSelector = ZeroTransformHelper<TSource, TResult, TResult>.ReturnItem
                });
        }

        /// <summary>
        /// Projects each element of a sequence to an IAsyncEnumerable&lt;T&gt; and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TItem">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence by <paramref name="resultSelector"/>.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TItem, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IEnumerable<TItem>> collectionSelector,
            Func<TSource, TItem, TResult> resultSelector)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == collectionSelector)
                throw new ArgumentNullException(nameof(collectionSelector));
            if (null == resultSelector)
                throw new ArgumentNullException(nameof(resultSelector));

            return new AsyncEnumerableWithState<TResult, SelectManySyncContext<TSource, TItem, TResult>>(
                SelectManySyncContext<TSource, TItem, TResult>.Enumerate,
                new SelectManySyncContext<TSource, TItem, TResult>
                {
                    Source = source,
                    CollectionSelector = collectionSelector,
                    ResultSelector = resultSelector
                });
        }

        private struct SelectManySyncContext<TSource, TItem, TResult>
        {
            public IAsyncEnumerable<TSource> Source;
            public Func<TSource, IEnumerable<TItem>> CollectionSelector;
            public Func<TSource, TItem, TResult> ResultSelector;

            private static async Task _enumerate(AsyncEnumerator<TResult>.Yield yield, SelectManySyncContext<TSource, TItem, TResult> context)
            {
                using (var sourceEnumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    while (await sourceEnumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        foreach (var intermediateItem in context.CollectionSelector(sourceEnumerator.Current))
                        {
                            var resultItem = context.ResultSelector(sourceEnumerator.Current, intermediateItem);
                            await yield.ReturnAsync(resultItem).ConfigureAwait(false);
                        }
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<TResult>.Yield, SelectManySyncContext<TSource, TItem, TResult>, Task> Enumerate = _enumerate;
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
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
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
        public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
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

        #region ToDictionary

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an <see cref="IAsyncEnumerable{T}"/> according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to create a <see cref="Dictionary{TKey, TValue}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation.</param>
        /// <returns></returns>
        public static async Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var dictionary = new Dictionary<TKey, TSource>();

            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var item = enumerator.Current;
                    dictionary.Add(keySelector(item), item);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an <see cref="IAsyncEnumerable{T}"/> according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to create a <see cref="Dictionary{TKey, TValue}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation.</param>
        /// <returns></returns>
        public static async Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            var dictionary = new Dictionary<TKey, TSource>(comparer);

            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var item = enumerator.Current;
                    dictionary.Add(keySelector(item), item);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an <see cref="IAsyncEnumerable{T}"/> according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to create a <see cref="Dictionary{TKey, TValue}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation.</param>
        /// <returns></returns>
        public static async Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null)
                throw new ArgumentNullException(nameof(elementSelector));

            var dictionary = new Dictionary<TKey, TElement>();

            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var item = enumerator.Current;
                    dictionary.Add(keySelector(item), elementSelector(item));
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an <see cref="IAsyncEnumerable{T}"/> according to a specified key selector function, a comparer, and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to create a <see cref="Dictionary{TKey, TValue}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation.</param>
        /// <returns></returns>
        public static async Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null)
                throw new ArgumentNullException(nameof(elementSelector));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            var dictionary = new Dictionary<TKey, TElement>(comparer);

            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var item = enumerator.Current;
                    dictionary.Add(keySelector(item), elementSelector(item));
                }
            }

            return dictionary;
        }

        #endregion

        #region ToLookup

        private class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            private readonly List<TElement> _items = new List<TElement>();

            public Grouping(TKey key)
            {
                Key = key;
            }

            public void Add(TElement item) => _items.Add(item);

            public int Count => _items.Count;

            public TKey Key { get; }

            public IEnumerator<TElement> GetEnumerator() => _items.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class Lookup<TKey, TElement> : ILookup<TKey, TElement>
        {
            private readonly Dictionary<TKey, Grouping<TKey, TElement>> _dictionary;

            public Lookup()
            {
                _dictionary = new Dictionary<TKey, Grouping<TKey, TElement>>();
            }

            public Lookup(IEqualityComparer<TKey> comparer)
            {
                _dictionary = new Dictionary<TKey, Grouping<TKey, TElement>>(comparer);
            }

            public void Add(TKey key, TElement item)
            {
                if (!_dictionary.TryGetValue(key, out var grouping))
                {
                    grouping = new Grouping<TKey, TElement>(key);
                    _dictionary.Add(grouping.Key, grouping);
                }
                grouping.Add(item);
                Count++;
            }

            public IEnumerable<TElement> this[TKey key]
            {
                get
                {
                    if (_dictionary.TryGetValue(key, out var grouping))
                        return grouping;
                    return Enumerable.Empty<TElement>();
                }
            }

            public int Count { get; private set; }

            public bool Contains(TKey key) => _dictionary.ContainsKey(key);

            public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator() => _dictionary.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Creates a <see cref="ILookup{TKey, TElement}"/> from an <see cref="IAsyncEnumerable{T}"/> according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to create a <see cref="ILookup{TKey, TElement}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation.</param>
        public static async Task<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            var lookup = new Lookup<TKey, TSource>();

            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var item = enumerator.Current;
                    lookup.Add(keySelector(item), item);
                }
            }

            return lookup;
        }

        /// <summary>
        /// Creates a <see cref="ILookup{TKey, TElement}"/> from an <see cref="IAsyncEnumerable{T}"/> according to a specified key selector function and key comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to create a <see cref="ILookup{TKey, TElement}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation.</param>
        public static async Task<ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            var lookup = new Lookup<TKey, TSource>(comparer);

            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var item = enumerator.Current;
                    lookup.Add(keySelector(item), item);
                }
            }

            return lookup;
        }

        /// <summary>
        /// Creates a <see cref="ILookup{TKey, TElement}"/> from an <see cref="IAsyncEnumerable{T}"/> according to a specified key selector function and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to create a <see cref="ILookup{TKey, TElement}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation.</param>
        public static async Task<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null)
                throw new ArgumentNullException(nameof(elementSelector));

            var lookup = new Lookup<TKey, TElement>();

            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var item = enumerator.Current;
                    lookup.Add(keySelector(item), elementSelector(item));
                }
            }

            return lookup;
        }

        /// <summary>
        /// Creates a <see cref="ILookup{TKey, TElement}"/> from an <see cref="IAsyncEnumerable{T}"/> according to a specified key selector function, a comparer and an element selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to create a <see cref="ILookup{TKey, TElement}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation.</param>
        public static async Task<ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null)
                throw new ArgumentNullException(nameof(elementSelector));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            var lookup = new Lookup<TKey, TElement>(comparer);

            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var item = enumerator.Current;
                    lookup.Add(keySelector(item), elementSelector(item));
                }
            }

            return lookup;
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

        #region OfType

        /// <summary>
        /// Filters the elements of an <see cref="IAsyncEnumerable"/> based on a specified type.
        /// </summary>
        /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable"/> whose elements to filter.</param>
        public static IAsyncEnumerable<TResult> OfType<TResult>(this IAsyncEnumerable source)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumerableWithState<TResult, OfTypeContext<TResult>>(
                OfTypeContext<TResult>.Enumerate,
                new OfTypeContext<TResult> { Source = source });
        }

        private struct OfTypeContext<TResult>
        {
            public IAsyncEnumerable Source;

            private static async Task _enumerate(AsyncEnumerator<TResult>.Yield yield, OfTypeContext<TResult> context)
            {
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                        if (enumerator.Current is TResult item)
                            await yield.ReturnAsync(item).ConfigureAwait(false);
            }

            public static readonly Func<AsyncEnumerator<TResult>.Yield, OfTypeContext<TResult>, Task> Enumerate = _enumerate;
        }

        #endregion

        #region DefaultIfEmpty

        /// <summary>
        /// Returns the elements of the specified sequence or the specified value in a singleton collection if the sequence is empty.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the specified value for if it is empty.</param>
        public static IAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this IAsyncEnumerable<TSource> source)
            => DefaultIfEmpty(source, default);

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
            => Batch<TSource, List<TSource>>(source, batchSize);

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
            => Batch(source, batchSize, long.MaxValue, null,
                BatchCollectionHelper<TSource>.GetCreateCollectionFunction<TStandardCollection>(),
                BatchCollectionHelper<TSource>.GetAddToCollectionAction<TStandardCollection>());

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
            => Batch<TSource, List<TSource>>(source, maxBatchWeight, weightSelector);

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
            => Batch(source, null, maxBatchWeight, weightSelector,
                BatchCollectionHelper<TSource>.GetCreateCollectionFunction<TStandardCollection>(),
                BatchCollectionHelper<TSource>.GetAddToCollectionAction<TStandardCollection>());

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
            => Batch<TSource, List<TSource>>(source, maxItemsInBatch, maxBatchWeight, weightSelector);

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
            => Batch(source, maxItemsInBatch, maxBatchWeight, weightSelector,
                BatchCollectionHelper<TSource>.GetCreateCollectionFunction<TStandardCollection>(),
                BatchCollectionHelper<TSource>.GetAddToCollectionAction<TStandardCollection>());

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

            if (maxItemsInBatch == null && weightSelector == null)
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
                            batch = default;
                            itemsInBatch = 0;
                            batchWeight = 0;
                        }

                        if (itemsInBatch == 0)
                            batch = context.CreateBatch(context.BatchPreallocateSize);

                        context.AddItemToBatch(batch, enumerator.Current);
                        batchWeight += itemWeight;
                        itemsInBatch += 1;

                        if (itemsInBatch >= context.MaxItemsInBatch || batchWeight >= context.MaxBatchWeight)
                        {
                            await yield.ReturnAsync(batch).ConfigureAwait(false);
                            batch = default;
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

        #region UnionAll

        /// <summary>
        /// Produces the set union of two sequences, which includes duplicate elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An <see cref="IAsyncEnumerable{T}"/> whose elements form the first set for the union.</param>
        /// <param name="second">An <see cref="IAsyncEnumerable{T}"/> whose elements form the second set for the union.</param>
        public static IAsyncEnumerable<T> UnionAll<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
        {
            if (null == first)
                throw new ArgumentNullException(nameof(first));
            if (null == second)
                throw new ArgumentNullException(nameof(second));

            return new AsyncEnumerableWithState<T, UnionContext<T>>(
                UnionContext<T>.Enumerate,
                new UnionContext<T> { Collections = new[] { first, second } });
        }

        /// <summary>
        /// Produces the set union of multiple sequences, which includes duplicate elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
        /// <param name="collections">A set of <see cref="IAsyncEnumerable{T}"/> whose elements form the union.</param>
        public static IAsyncEnumerable<T> UnionAll<T>(this IEnumerable<IAsyncEnumerable<T>> collections)
        {
            if (null == collections)
                throw new ArgumentNullException(nameof(collections));

            return new AsyncEnumerableWithState<T, UnionContext<T>>(
                UnionContext<T>.Enumerate,
                new UnionContext<T> { Collections = collections });
        }

        private struct UnionContext<T>
        {
            public IEnumerable<IAsyncEnumerable<T>> Collections;

            private static async Task _enumerate(AsyncEnumerator<T>.Yield yield, UnionContext<T> context)
            {
                foreach (var collection in context.Collections)
                {
                    if (collection == null)
                        continue;

                    using (var enumerator = await collection.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                            await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<T>.Yield, UnionContext<T>, Task> Enumerate = _enumerate;
        }

        #endregion

        #region Append / Prepend

        /// <summary>
        /// Creates a new sequence based on input one plus an extra element at the end.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="element">An extra element to be returned on enumeration.</param>
        public static IAsyncEnumerable<TSource> Append<TSource>(
            this IAsyncEnumerable<TSource> source, TSource element)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumerableWithState<TSource, ExtraElementContext<TSource>>(
                ExtraElementContext<TSource>.Enumerate,
                new ExtraElementContext<TSource> { Source = source, ExtraElement = element, Append = true });
        }

        /// <summary>
        /// Creates a new sequence based on input one plus an extra element in the beginning.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="element">An extra element to be returned on enumeration.</param>
        public static IAsyncEnumerable<TSource> Prepend<TSource>(
            this IAsyncEnumerable<TSource> source, TSource element)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumerableWithState<TSource, ExtraElementContext<TSource>>(
                ExtraElementContext<TSource>.Enumerate,
                new ExtraElementContext<TSource> { Source = source, ExtraElement = element, Prepend = true });
        }

        private struct ExtraElementContext<TSource>
        {
            public IAsyncEnumerable<TSource> Source;
            public TSource ExtraElement;
            public bool Append;
            public bool Prepend { get => !Append; set => Append = !value; }

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, ExtraElementContext<TSource> context)
            {
                if (context.Prepend)
                    await yield.ReturnAsync(context.ExtraElement).ConfigureAwait(false);

                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                    }
                }

                if (context.Append)
                    await yield.ReturnAsync(context.ExtraElement).ConfigureAwait(false);
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, ExtraElementContext<TSource>, Task> Enumerate = _enumerate;
        }

        #endregion

        #region Concat

        /// <summary>
        /// Concatenates two sequences.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        public static IAsyncEnumerable<TSource> Concat<TSource>(
            this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second)
        {
            if (null == first)
                throw new ArgumentNullException(nameof(first));
            if (null == second)
                throw new ArgumentNullException(nameof(second));

            return new AsyncEnumerableWithState<TSource, ConcatContext<TSource>>(
                ConcatContext<TSource>.Enumerate,
                new ConcatContext<TSource> { First = first, Second = second });
        }

        private struct ConcatContext<TSource>
        {
            public IAsyncEnumerable<TSource> First;
            public IAsyncEnumerable<TSource> Second;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, ConcatContext<TSource> context)
            {
                using (var enumerator = await context.First.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                    }
                }

                using (var enumerator = await context.Second.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                {
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, ConcatContext<TSource>, Task> Enumerate = _enumerate;
        }

        #endregion

        #region Distinct

        /// <summary>
        /// Returns distinct elements from a sequence by using the default equality comparer to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        public static IAsyncEnumerable<TSource> Distinct<TSource>(this IAsyncEnumerable<TSource> source)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumerableWithState<TSource, DistinctContext<TSource>>(
                DistinctContext<TSource>.Enumerate,
                new DistinctContext<TSource> { Source = source });
        }

        /// <summary>
        /// Returns distinct elements from a sequence by using a specified <see cref="IEqualityComparer{T}"/> to compare values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        public static IAsyncEnumerable<TSource> Distinct<TSource>(this IAsyncEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == comparer)
                throw new ArgumentNullException(nameof(comparer));

            return new AsyncEnumerableWithState<TSource, DistinctContext<TSource>>(
                DistinctContext<TSource>.Enumerate,
                new DistinctContext<TSource> { Source = source, Comparer = comparer });
        }

        private struct DistinctContext<TSource>
        {
            public IAsyncEnumerable<TSource> Source;
            public IEqualityComparer<TSource> Comparer;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, DistinctContext<TSource> context)
            {
                var set = context.Comparer == null ? new HashSet<TSource>() : new HashSet<TSource>(context.Comparer);
                using (var enumerator = await context.Source.GetAsyncEnumeratorAsync(yield.CancellationToken).ConfigureAwait(false))
                    while (await enumerator.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                        if (set.Add(enumerator.Current))
                            await yield.ReturnAsync(enumerator.Current).ConfigureAwait(false);
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, DistinctContext<TSource>, Task> Enumerate = _enumerate;
        }

        #endregion

        #region Aggregate

        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source"> An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation.</param>
        public static async Task<TSource> AggregateAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TSource, TSource> func,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            TSource val;
            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException("The sequence contains no elements.");
                val = enumerator.Current;
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    val = func(val, enumerator.Current);
            }
            return val;
        }

        /// <summary>
        /// Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source"> An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation.</param>
        public static async Task<TAccumulate> AggregateAsync<TSource, TAccumulate>(
            this IAsyncEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            var val = seed;
            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    val = func(val, enumerator.Current);
            return val;
        }

        /// <summary>
        /// Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value, and the specified function is used to select the result value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source"> An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation.</param>
        public static async Task<TResult> AggregateAsync<TSource, TAccumulate, TResult>(
            this IAsyncEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            Func<TAccumulate, TResult> resultSelector,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            if (resultSelector == null)
                throw new ArgumentNullException(nameof(resultSelector));

            var val = seed;
            using (var enumerator = await source.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    val = func(val, enumerator.Current);
            return resultSelector(val);
        }

        #endregion

        #region Shared Helpers

        internal static class ZeroTransformHelper
        {
            internal static TResult _returnItem<TSource, TItem, TResult>(TSource source, TItem item)
                where TItem : TResult => item;
        }

        internal static class ZeroTransformHelper<TSource, TItem, TResult> where TItem : TResult
        {
            public static readonly Func<TSource, TItem, TResult> ReturnItem =
                ZeroTransformHelper._returnItem<TSource, TItem, TResult>;
        }

        #endregion
    }
}
