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
            CancellationToken token = default(CancellationToken),
            bool disposeSource = true)
        {
            return FirstAsync(source, PredicateCache<TSource>.True, token, disposeSource);
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerator{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="token">A <see cref="CancellationToken"/> that can halt enumeration of <paramref name="source"/></param>
        /// <param name="disposeSource">Flag to call the <see cref="IDisposable.Dispose"/> on input <paramref name="source"/> when this operation is complete</param>
        public static async Task<TSource> FirstAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken token = default(CancellationToken),
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

                throw new InvalidOperationException("No Matching Element Found");
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
            CancellationToken token = default(CancellationToken),
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
            CancellationToken token = default(CancellationToken),
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

                return default(TSource);
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
        public static IAsyncEnumerator<TResult> SelectAsync<TSource, TResult>(
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
                new SelectContext<TSource, TResult> { Source = source, Selector = selector, DisposeSource = disposeSource },
                oneTimeUse: true);
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
        public static IAsyncEnumerator<TResult> SelectAsync<TSource, TResult>(
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
                new SelectWithIndexContext<TSource, TResult> { Source = source, Selector = selector, DisposeSource = disposeSource },
                oneTimeUse: true);
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
        public static IAsyncEnumerator<TSource> TakeAsync<TSource>(
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
                new TakeContext<TSource> { Source = source, Count = count, DisposeSource = disposeSource },
                oneTimeUse: true);
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
        public static IAsyncEnumerator<TSource> TakeWhileAsync<TSource>(
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
                new TakeWhileContext<TSource> { Source = source, Predicate = predicate, DisposeSource = disposeSource },
                oneTimeUse: true);
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
            CancellationToken cancellationToken = default(CancellationToken),
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
            CancellationToken cancellationToken = default(CancellationToken),
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
        public static IAsyncEnumerator<TSource> SkipAsync<TSource>(
            this IAsyncEnumerator<TSource> source,
            int count,
            bool disposeSource = true)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            return new AsyncEnumeratorWithState<TSource, SkipContext<TSource>>(
                SkipContext<TSource>.Enumerate,
                new SkipContext<TSource> { Source = source, Count = count, DisposeSource = disposeSource },
                oneTimeUse: true);
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
        public static IAsyncEnumerator<TSource> SkipWhileAsync<TSource>(
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
                new SkipWhileContext<TSource> { Source = source, Predicate = predicate, DisposeSource = disposeSource },
                oneTimeUse: true);
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
        public static IAsyncEnumerator<TSource> WhereAsync<TSource>(
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
                new WhereContext<TSource> { Source = source, Predicate = predicate, DisposeSource = disposeSource },
                oneTimeUse: true);
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
        public static IAsyncEnumerator<TSource> WhereAsync<TSource>(
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
                new WhereWithIndexContext<TSource> { Source = source, Predicate = predicate, DisposeSource = disposeSource },
                oneTimeUse: true);
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
    }
}
