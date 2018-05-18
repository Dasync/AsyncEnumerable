using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Async.IAsyncEnumerableExtensions;

namespace System.Collections.Async
{
    /// <summary>
    /// Extension methods for <see cref="IAsyncEnumerator{T}"/> interface
    /// </summary>
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    public static class IAsyncEnumeratorExtensions
    {
        #region Single / SingleOrDefault

        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/>.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static Task<TSource> SingleAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            return SingleAsync(source, PredicateCache<TSource>.True, null, null, token, disposeSource);
        }

        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="noneExceptionMessage">The message of an exception which is thrown when the source collection is empty.</param>
        /// <param name="manyExceptionMessage">The message of an exception which is thrown when the source collection has more than one element.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/>.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static Task<TSource> SingleAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            string noneExceptionMessage,
            string manyExceptionMessage,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            return SingleAsync(source, PredicateCache<TSource>.True, noneExceptionMessage, manyExceptionMessage, token, disposeSource);
        }

        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence that matches the criteria.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="predicate">Criteria predicate to select the only element.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/>.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static Task<TSource> SingleAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            return SingleAsync(source, predicate, null, null, token, disposeSource);
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
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static async Task<TSource> SingleAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, bool> predicate,
            string noneExceptionMessage,
            string manyExceptionMessage,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            var matchFound = false;
            var lastMatch = default(TSource);

            try
            {
                while (await source.MoveNextAsync(token).ConfigureAwait(false))
                {
                    if (predicate(source.Current))
                    {
                        if (matchFound)
                            throw new InvalidOperationException(string.IsNullOrEmpty(manyExceptionMessage) ? "Several elements found matching the criteria." : manyExceptionMessage);

                        matchFound = true;
                        lastMatch = source.Current;
                    }
                }
            }
            finally
            {
                if (disposeSource)
                    source.Dispose();
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
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static Task<TSource> SingleOrDefaultAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            return SingleOrDefaultAsync(source, PredicateCache<TSource>.True, token, disposeSource);
        }

        /// <summary>
        /// Returns the only element of a sequence, and returns a default value if there is not exactly one element in the sequence that matches the criteria.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="predicate">Criteria predicate to select the only element.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/>.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static async Task<TSource> SingleOrDefaultAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            var matchFound = false;
            var lastMatch = default(TSource);

            try
            {
                while (await source.MoveNextAsync(token).ConfigureAwait(false))
                {
                    if (predicate(source.Current))
                    {
                        if (matchFound)
                        {
                            matchFound = false;
                            break;
                        }

                        matchFound = true;
                        lastMatch = source.Current;
                    }
                }
            }
            finally
            {
                if (disposeSource)
                    source.Dispose();
            }

            if (!matchFound)
                return default;

            return lastMatch;
        }

        #endregion

        #region First / FirstOrDefault

        /// <summary>
        /// Returns the first element in the <see cref="IAsyncEnumerator{T}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to return an element from.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static Task<TSource> FirstAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            return FirstAsync(source, PredicateCache<TSource>.True, null, token, disposeSource);
        }

        /// <summary>
        /// Returns the first element in the <see cref="IAsyncEnumerator{T}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to return an element from.</param>
        /// <param name="exceptionMessage">An optional custom exception message for the case when the <paramref name="source"/> is empty</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static Task<TSource> FirstAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            string exceptionMessage,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            return FirstAsync(source, PredicateCache<TSource>.True, exceptionMessage, token, disposeSource);
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static Task<TSource> FirstAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            return FirstAsync(source, predicate, null, token, disposeSource);
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="exceptionMessage">An optional custom exception message for the case when the <paramref name="source"/> is empty</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static async Task<TSource> FirstAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, bool> predicate,
            string exceptionMessage,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            try
            {
                if (null == source)
                    throw new ArgumentNullException(nameof(source));
                if (null == predicate)
                    throw new ArgumentNullException(nameof(predicate));

                while (await source.MoveNextAsync(token).ConfigureAwait(false))
                    if (predicate(source.Current))
                        return source.Current;

                throw new InvalidOperationException(string.IsNullOrEmpty(exceptionMessage) ? "No Matching Element Found" : exceptionMessage);
            }
            finally
            {
                if (disposeSource)
                    source.Dispose();
            }
        }

        /// <summary>
        /// Returns the first element in the <see cref="IAsyncEnumerator{T}"/>, or a default value if no element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to return an element from.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static Task<TSource> FirstOrDefaultAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            return FirstOrDefaultAsync(source, PredicateCache<TSource>.True, token, disposeSource);
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition, or a default value if no element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static async Task<TSource> FirstOrDefaultAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken token = default,
            bool disposeSource = true)
        {
            try
            {
                if (null == source)
                    throw new ArgumentNullException(nameof(source));
                if (null == predicate)
                    throw new ArgumentNullException(nameof(predicate));

                while (await source.MoveNextAsync(token).ConfigureAwait(false))
                    if (predicate(source.Current))
                        return source.Current;

                return default;
            }
            finally
            {
                if (disposeSource)
                    source.Dispose();
            }
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
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TResult> Select<TSource, TResult>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, TResult> selector,
            bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == selector)
                throw new ArgumentNullException(nameof(selector));

            return new AsyncEnumeratorWithState<TResult, SelectContext<TSource, TResult>>(
                SelectContext<TSource, TResult>.Enumerate,
                new SelectContext<TSource, TResult> { Source = source, Selector = selector, DisposeSource = disposeSource });
        }

        private struct SelectContext<TSource, TResult>
        {
            public IAsyncEnumerator<TSource> Source;
            public Func<TSource, TResult> Selector;
            public bool DisposeSource;

            private static async Task _enumerate(AsyncEnumerator<TResult>.Yield yield, SelectContext<TSource, TResult> context)
            {
                try
                {
                    while (await context.Source.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        await yield.ReturnAsync(context.Selector(context.Source.Current)).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (context.DisposeSource)
                        context.Source.Dispose();
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
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TResult> Select<TSource, TResult>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, long, TResult> selector,
            bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == selector)
                throw new ArgumentNullException(nameof(selector));

            return new AsyncEnumeratorWithState<TResult, SelectWithIndexContext<TSource, TResult>>(
                SelectWithIndexContext<TSource, TResult>.Enumerate,
                new SelectWithIndexContext<TSource, TResult> { Source = source, Selector = selector, DisposeSource = disposeSource });
        }

        private struct SelectWithIndexContext<TSource, TResult>
        {
            public IAsyncEnumerator<TSource> Source;
            public Func<TSource, long, TResult> Selector;
            public bool DisposeSource;

            private static async Task _enumerate(AsyncEnumerator<TResult>.Yield yield, SelectWithIndexContext<TSource, TResult> context)
            {
                try
                {
                    long index = 0;
                    while (await context.Source.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        await yield.ReturnAsync(context.Selector(context.Source.Current, index)).ConfigureAwait(false);
                        index++;
                    }
                }
                finally
                {
                    if (context.DisposeSource)
                        context.Source.Dispose();
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
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TSource> Take<TSource>(
            this IAsyncEnumerator<TSource> source,
            int count,
            bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            if (count <= 0)
                return AsyncEnumerator<TSource>.Empty;

            return new AsyncEnumeratorWithState<TSource, TakeContext<TSource>>(
                TakeContext<TSource>.Enumerate,
                new TakeContext<TSource> { Source = source, Count = count, DisposeSource = disposeSource });
        }

        private struct TakeContext<TSource>
        {
            public IAsyncEnumerator<TSource> Source;
            public int Count;
            public bool DisposeSource;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, TakeContext<TSource> context)
            {
                try
                {
                    for (var i = context.Count; i > 0; i--)
                    {
                        if (await context.Source.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                            await yield.ReturnAsync(context.Source.Current).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (context.DisposeSource)
                        context.Source.Dispose();
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
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TSource> TakeWhile<TSource>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, bool> predicate,
            bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumeratorWithState<TSource, TakeWhileContext<TSource>>(
                TakeWhileContext<TSource>.Enumerate,
                new TakeWhileContext<TSource> { Source = source, Predicate = predicate, DisposeSource = disposeSource });
        }

        private struct TakeWhileContext<TSource>
        {
            public IAsyncEnumerator<TSource> Source;
            public Func<TSource, bool> Predicate;
            public bool DisposeSource;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, TakeWhileContext<TSource> context)
            {
                try
                {
                    while (await context.Source.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (context.Predicate(context.Source.Current))
                            await yield.ReturnAsync(context.Source.Current).ConfigureAwait(false);
                        else
                            break;
                    }
                }
                finally
                {
                    if (context.DisposeSource)
                        context.Source.Dispose();
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
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static async Task<List<T>> ToListAsync<T>(
            this IAsyncEnumerator<T> source,
            CancellationToken cancellationToken = default,
            bool disposeSource = true)
        {
            try
            {
                var resultList = new List<T>();
                while (await source.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    resultList.Add(source.Current);
                return resultList;
            }
            finally
            {
                if (disposeSource)
                    source.Dispose();
            }
        }

        #endregion

        #region ToArray

        /// <summary>
        /// Creates an array of elements asynchronously from the enumerable source
        /// </summary>
        /// <typeparam name="T">The type of the elements of source</typeparam>
        /// <param name="source">The collection of elements</param>
        /// <param name="cancellationToken">A cancellation token to cancel the async operation</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static async Task<T[]> ToArrayAsync<T>(
            this IAsyncEnumerator<T> source,
            CancellationToken cancellationToken = default,
            bool disposeSource = true)
        {
            try
            {
                var resultList = new List<T>();
                while (await source.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    resultList.Add(source.Current);
                return resultList.ToArray();
            }
            finally
            {
                if (disposeSource)
                    source.Dispose();
            }
        }

        #endregion

        #region Skip / SkipWhile

        /// <summary>
        /// An <see cref="IAsyncEnumerator{T}"/> to return elements from.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TSource> Skip<TSource>(
            this IAsyncEnumerator<TSource> source,
            int count,
            bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumeratorWithState<TSource, SkipContext<TSource>>(
                SkipContext<TSource>.Enumerate,
                new SkipContext<TSource> { Source = source, Count = count, DisposeSource = disposeSource });
        }

        private struct SkipContext<TSource>
        {
            public IAsyncEnumerator<TSource> Source;
            public int Count;
            public bool DisposeSource;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, SkipContext<TSource> context)
            {
                try
                {
                    var itemsToSkip = context.Count;
                    while (await context.Source.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (itemsToSkip > 0)
                            itemsToSkip--;
                        else
                            await yield.ReturnAsync(context.Source.Current).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (context.DisposeSource)
                        context.Source.Dispose();
                }
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, SkipContext<TSource>, Task> Enumerate = _enumerate;
        }

        /// <summary>
        /// Bypasses elements in a sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TSource> SkipWhile<TSource>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, bool> predicate,
            bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumeratorWithState<TSource, SkipWhileContext<TSource>>(
                SkipWhileContext<TSource>.Enumerate,
                new SkipWhileContext<TSource> { Source = source, Predicate = predicate, DisposeSource = disposeSource });
        }

        private struct SkipWhileContext<TSource>
        {
            public IAsyncEnumerator<TSource> Source;
            public Func<TSource, bool> Predicate;
            public bool DisposeSource;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, SkipWhileContext<TSource> context)
            {
                try
                {
                    var yielding = false;
                    while (await context.Source.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (!yielding && !context.Predicate(context.Source.Current))
                            yielding = true;

                        if (yielding)
                            await yield.ReturnAsync(context.Source.Current).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (context.DisposeSource)
                        context.Source.Dispose();
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
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TSource> Where<TSource>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, bool> predicate,
            bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumeratorWithState<TSource, WhereContext<TSource>>(
                WhereContext<TSource>.Enumerate,
                new WhereContext<TSource> { Source = source, Predicate = predicate, DisposeSource = disposeSource });
        }

        private struct WhereContext<TSource>
        {
            public IAsyncEnumerator<TSource> Source;
            public Func<TSource, bool> Predicate;
            public bool DisposeSource;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, WhereContext<TSource> context)
            {
                try
                {
                    while (await context.Source.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (context.Predicate(context.Source.Current))
                            await yield.ReturnAsync(context.Source.Current).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (context.DisposeSource)
                        context.Source.Dispose();
                }
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, WhereContext<TSource>, Task> Enumerate = _enumerate;
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to filter.</param>
        /// <param name="predicate">A function to test each element for a condition; the second parameter of the function represents the index of the source element.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TSource> Where<TSource>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, long, bool> predicate,
            bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));
            if (null == predicate)
                throw new ArgumentNullException(nameof(predicate));

            return new AsyncEnumeratorWithState<TSource, WhereWithIndexContext<TSource>>(
                WhereWithIndexContext<TSource>.Enumerate,
                new WhereWithIndexContext<TSource> { Source = source, Predicate = predicate, DisposeSource = disposeSource });
        }

        private struct WhereWithIndexContext<TSource>
        {
            public IAsyncEnumerator<TSource> Source;
            public Func<TSource, long, bool> Predicate;
            public bool DisposeSource;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, WhereWithIndexContext<TSource> context)
            {
                try
                {
                    long index = 0;
                    while (await context.Source.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        if (context.Predicate(context.Source.Current, index))
                            await yield.ReturnAsync(context.Source.Current).ConfigureAwait(false);
                        index++;
                    }
                }
                finally
                {
                    if (context.DisposeSource)
                        context.Source.Dispose();
                }
            }

            public static readonly Func<AsyncEnumerator<TSource>.Yield, WhereWithIndexContext<TSource>, Task> Enumerate = _enumerate;
        }

        #endregion

        #region Cast

        /// <summary>
        /// Casts the elements of an <see cref="IAsyncEnumerator"/> to the specified type.
        /// </summary>
        /// <typeparam name="TResult">The type to cast the elements of <paramref name="source"/> to.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator"/> that contains the elements to be cast to type <typeparamref name="TResult"/>.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TResult> Cast<TResult>(this IAsyncEnumerator source, bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumeratorWithState<TResult, CastContext<TResult>>(
                CastContext<TResult>.Enumerate,
                new CastContext<TResult> { Source = source, DisposeSource = disposeSource });
        }

        private struct CastContext<TResult>
        {
            public IAsyncEnumerator Source;
            public bool DisposeSource;

            private static async Task _enumerate(AsyncEnumerator<TResult>.Yield yield, CastContext<TResult> context)
            {
                try
                {
                    while (await context.Source.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                        await yield.ReturnAsync((TResult)context.Source.Current).ConfigureAwait(false);
                }
                finally
                {
                    if (context.DisposeSource)
                        context.Source.Dispose();
                }
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
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TSource> DefaultIfEmpty<TSource>(this IAsyncEnumerator<TSource> source, bool disposeSource = true)
        {
            return DefaultIfEmpty(source, default, disposeSource);
        }

        /// <summary>
        /// Returns the elements of the specified sequence or the specified value in a singleton collection if the sequence is empty.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to return the specified value for if it is empty.</param>
        /// <param name="defaultValue">The value to return if the sequence is empty.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TSource> DefaultIfEmpty<TSource>(this IAsyncEnumerator<TSource> source, TSource defaultValue, bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumeratorWithState<TSource, DefaultIfEmptyContext<TSource>>(
                DefaultIfEmptyContext<TSource>.Enumerate,
                new DefaultIfEmptyContext<TSource> { Source = source, DefaultValue = defaultValue, DisposeSource = disposeSource });
        }

        private struct DefaultIfEmptyContext<TSource>
        {
            public IAsyncEnumerator<TSource> Source;
            public TSource DefaultValue;
            public bool DisposeSource;

            private static async Task _enumerate(AsyncEnumerator<TSource>.Yield yield, DefaultIfEmptyContext<TSource> context)
            {
                try
                {
                    var isEmpty = true;

                    while (await context.Source.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        isEmpty = false;
                        await yield.ReturnAsync(context.Source.Current).ConfigureAwait(false);
                    }

                    if (isEmpty)
                        await yield.ReturnAsync(context.DefaultValue).ConfigureAwait(false);
                }
                finally
                {
                    if (context.DisposeSource)
                        context.Source.Dispose();
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
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to batch.</param>
        /// <param name="batchSize">The maximum number of elements to put in a batch.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<List<TSource>> Batch<TSource>(
            this IAsyncEnumerator<TSource> source,
            int batchSize,
            bool disposeSource = true)
        {
            return Batch<TSource, List<TSource>>(source, batchSize, disposeSource);
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
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to batch.</param>
        /// <param name="batchSize">The maximum number of elements to put in a batch.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TStandardCollection> Batch<TSource, TStandardCollection>(
            this IAsyncEnumerator<TSource> source,
            int batchSize,
            bool disposeSource = true)
        {
            return Batch(source, batchSize, long.MaxValue, null,
                BatchCollectionHelper<TSource>.GetCreateCollectionFunction<TStandardCollection>(),
                BatchCollectionHelper<TSource>.GetAddToCollectionAction<TStandardCollection>(),
                disposeSource);
        }

        /// <summary>
        /// Splits the input collection into series of batches.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to batch.</param>
        /// <param name="maxBatchWeight">The maximum logical weight of elements that a single batch can accomodate.</param>
        /// <param name="weightSelector">A function that computes a weight of a particular element, which is used to make a decision if it can fit into a batch.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<List<TSource>> Batch<TSource>(
            this IAsyncEnumerator<TSource> source,
            long maxBatchWeight,
            Func<TSource, long> weightSelector,
            bool disposeSource = true)
        {
            return Batch<TSource, List<TSource>>(source, maxBatchWeight, weightSelector, disposeSource);
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
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to batch.</param>
        /// <param name="maxBatchWeight">The maximum logical weight of elements that a single batch can accomodate.</param>
        /// <param name="weightSelector">A function that computes a weight of a particular element, which is used to make a decision if it can fit into a batch.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TStandardCollection> Batch<TSource, TStandardCollection>(
            this IAsyncEnumerator<TSource> source,
            long maxBatchWeight,
            Func<TSource, long> weightSelector,
            bool disposeSource = true)
        {
            return Batch(source, null, maxBatchWeight, weightSelector,
                BatchCollectionHelper<TSource>.GetCreateCollectionFunction<TStandardCollection>(),
                BatchCollectionHelper<TSource>.GetAddToCollectionAction<TStandardCollection>(),
                disposeSource);
        }

        /// <summary>
        /// Splits the input collection into series of batches.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to batch.</param>
        /// <param name="maxItemsInBatch">The maximum number of elements to put in a batch regardless their total weight.</param>
        /// <param name="maxBatchWeight">The maximum logical weight of elements that a single batch can accomodate.</param>
        /// <param name="weightSelector">A function that computes a weight of a particular element, which is used to make a decision if it can fit into a batch.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<List<TSource>> Batch<TSource>(
            this IAsyncEnumerator<TSource> source,
            int maxItemsInBatch,
            long maxBatchWeight,
            Func<TSource, long> weightSelector,
            bool disposeSource = true)
        {
            return Batch<TSource, List<TSource>>(source, maxItemsInBatch, maxBatchWeight, weightSelector, disposeSource);
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
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to batch.</param>
        /// <param name="maxItemsInBatch">The maximum number of elements to put in a batch regardless their total weight.</param>
        /// <param name="maxBatchWeight">The maximum logical weight of elements that a single batch can accomodate.</param>
        /// <param name="weightSelector">A function that computes a weight of a particular element, which is used to make a decision if it can fit into a batch.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TStandardCollection> Batch<TSource, TStandardCollection>(
            this IAsyncEnumerator<TSource> source,
            int maxItemsInBatch,
            long maxBatchWeight,
            Func<TSource, long> weightSelector,
            bool disposeSource = true)
        {
            return Batch(source, maxItemsInBatch, maxBatchWeight, weightSelector,
                BatchCollectionHelper<TSource>.GetCreateCollectionFunction<TStandardCollection>(),
                BatchCollectionHelper<TSource>.GetAddToCollectionAction<TStandardCollection>(),
                disposeSource);
        }

        /// <summary>
        /// Splits the input collection into series of batches.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TBatch">The type of a batch of elements.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to batch.</param>
        /// <param name="maxItemsInBatch">The maximum number of elements to put in a batch regardless their total weight.</param>
        /// <param name="maxBatchWeight">The maximum logical weight of elements that a single batch can accomodate.</param>
        /// <param name="weightSelector">A function that computes a weight of a particular element, which is used to make a decision if it can fit into a batch.</param>
        /// <param name="createBatch">A function that creates a new batch with optional suggested capacity.</param>
        /// <param name="addItem">An action that adds an element to a batch.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when enumeration is complete</param>
        public static IAsyncEnumerator<TBatch> Batch<TSource, TBatch>(
            this IAsyncEnumerator<TSource> source,
            int? maxItemsInBatch,
            long maxBatchWeight,
            Func<TSource, long> weightSelector,
            Func<int?, TBatch> createBatch,
            Action<TBatch, TSource> addItem,
            bool disposeSource = true)
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

            return new AsyncEnumeratorWithState<TBatch, BatchContext<TSource, TBatch>>(
                BatchContext<TSource, TBatch>.Enumerate,
                new BatchContext<TSource, TBatch>
                {
                    CreateBatch = createBatch,
                    AddItemToBatch = addItem,
                    BatchPreallocateSize = maxItemsInBatch,
                    MaxItemsInBatch = maxItemsInBatch ?? int.MaxValue,
                    MaxBatchWeight = maxBatchWeight,
                    WeightSelector = weightSelector,
                    Source = source,
                    DisposeSource = disposeSource
                });
        }

        private struct BatchContext<TSource, TBatch>
        {
            public IAsyncEnumerator<TSource> Source;
            public bool DisposeSource;
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

                try
                {
                    while (await context.Source.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                    {
                        var itemWeight = context.WeightSelector?.Invoke(context.Source.Current) ?? 0L;

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

                        context.AddItemToBatch(batch, context.Source.Current);
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
                finally
                {
                    if (context.DisposeSource)
                        context.Source.Dispose();
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
        /// <param name="first">An <see cref="IAsyncEnumerator{T}"/> whose elements form the first set for the union.</param>
        /// <param name="second">An <see cref="IAsyncEnumerator{T}"/> whose elements form the second set for the union.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="first"/> and <paramref name="second"/> when enumeration is complete.</param>
        public static IAsyncEnumerator<T> UnionAll<T>(this IAsyncEnumerator<T> first, IAsyncEnumerator<T> second, bool disposeSource = true)
        {
            if (null == first)
                throw new ArgumentNullException(nameof(first));
            if (null == second)
                throw new ArgumentNullException(nameof(second));

            return new AsyncEnumeratorWithState<T, UnionContext<T>>(
                UnionContext<T>.Enumerate,
                new UnionContext<T> { Collections = new[] { first, second }, DisposeSource = disposeSource });
        }

        /// <summary>
        /// Produces the set union of multiple sequences, which includes duplicate elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
        /// <param name="collections">A set of <see cref="IAsyncEnumerator{T}"/> whose elements form the union.</param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on all input <paramref name="collections"/> when enumeration is complete.</param>
        public static IAsyncEnumerator<T> UnionAll<T>(this IEnumerable<IAsyncEnumerator<T>> collections, bool disposeSource = true)
        {
            if (null == collections)
                throw new ArgumentNullException(nameof(collections));

            return new AsyncEnumeratorWithState<T, UnionContext<T>>(
                UnionContext<T>.Enumerate,
                new UnionContext<T> { Collections = collections, DisposeSource = disposeSource });
        }

        private struct UnionContext<T>
        {
            public IEnumerable<IAsyncEnumerator<T>> Collections;
            public bool DisposeSource;

            private static async Task _enumerate(AsyncEnumerator<T>.Yield yield, UnionContext<T> context)
            {
                foreach (var collection in context.Collections)
                {
                    if (collection == null)
                        continue;

                    try
                    {
                        while (await collection.MoveNextAsync(yield.CancellationToken).ConfigureAwait(false))
                            await yield.ReturnAsync(collection.Current).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (context.DisposeSource)
                            collection.Dispose();
                    }
                }
            }

            public static readonly Func<AsyncEnumerator<T>.Yield, UnionContext<T>, Task> Enumerate = _enumerate;
        }

        #endregion
    }
}
