using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    /// <summary>
    /// Enables asynchronous 'foreach' enumeration over an IAsyncEnumerable
    /// </summary>
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    public static class ForEachAsyncExtensions
    {
        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <param name="enumerable">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">A synchronous action to perform for every single item in the collection</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync(this IAsyncEnumerable enumerable, Action<object> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    action(enumerator.Current);
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (var enumerator = await enumerable.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    action(enumerator.Current);
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <param name="enumerator">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">A synchronous action to perform for every single item in the collection</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync(this IAsyncEnumerator enumerator, Action<object> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    action(enumerator.Current);
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (enumerator)
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    action(enumerator.Current);
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <param name="enumerable">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">A synchronous action to perform for every single item in the collection, where the second argument is the index of an item</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync(this IAsyncEnumerable enumerable, Action<object, long> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
            try
            {
                long index = 0;

                while (await enumerator.MoveNextAsync())
                {
                    action(enumerator.Current, index);
                    index++;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (var enumerator = await enumerable.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                long index = 0;

                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    action(enumerator.Current, index);
                    index++;
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <param name="enumerator">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">A synchronous action to perform for every single item in the collection, where the second argument is the index of an item</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync(this IAsyncEnumerator enumerator, Action<object, long> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            try
            {
                long index = 0;

                while (await enumerator.MoveNextAsync())
                {
                    action(enumerator.Current, index);
                    index++;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (enumerator)
            {
                long index = 0;

                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    action(enumerator.Current, index);
                    index++;
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <param name="enumerable">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">An asynchronous action to perform for every single item in the collection</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync(this IAsyncEnumerable enumerable, Func<object, Task> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    await action(enumerator.Current).ConfigureAwait(false);
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (var enumerator = await enumerable.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    await action(enumerator.Current).ConfigureAwait(false);
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <param name="enumerator">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">An asynchronous action to perform for every single item in the collection</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync(this IAsyncEnumerator enumerator, Func<object, Task> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    await action(enumerator.Current).ConfigureAwait(false);
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (enumerator)
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    await action(enumerator.Current).ConfigureAwait(false);
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <param name="enumerable">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">An asynchronous action to perform for every single item in the collection, where the second argument is the index of an item</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync(this IAsyncEnumerable enumerable, Func<object, long, Task> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
            try
            {
                long index = 0;

                while (await enumerator.MoveNextAsync())
                {
                    await action(enumerator.Current, index).ConfigureAwait(false);
                    index++;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (var enumerator = await enumerable.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                long index = 0;

                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    await action(enumerator.Current, index).ConfigureAwait(false);
                    index++;
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <param name="enumerator">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">An asynchronous action to perform for every single item in the collection, where the second argument is the index of an item</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync(this IAsyncEnumerator enumerator, Func<object, long, Task> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            try
            {
                long index = 0;

                while (await enumerator.MoveNextAsync())
                {
                    await action(enumerator.Current, index).ConfigureAwait(false);
                    index++;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (enumerator)
            {
                long index = 0;

                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    await action(enumerator.Current, index).ConfigureAwait(false);
                    index++;
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="enumerable">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">A synchronous action to perform for every single item in the collection</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> enumerable, Action<T> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    action(enumerator.Current);
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (var enumerator = await enumerable.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    action(enumerator.Current);
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="enumerator">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">A synchronous action to perform for every single item in the collection</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync<T>(this IAsyncEnumerator<T> enumerator, Action<T> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            try
            {
                action(enumerator.Current);
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (enumerator)
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    action(enumerator.Current);
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="enumerable">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">A synchronous action to perform for every single item in the collection, where the second argument is the index of an item</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> enumerable, Action<T, long> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
            try
            {
                long index = 0;

                while (await enumerator.MoveNextAsync())
                {
                    action(enumerator.Current, index);
                    index++;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (var enumerator = await enumerable.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                long index = 0;

                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    action(enumerator.Current, index);
                    index++;
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="enumerator">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">A synchronous action to perform for every single item in the collection, where the second argument is the index of an item</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync<T>(this IAsyncEnumerator<T> enumerator, Action<T, long> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            try
            {
                long index = 0;

                while (await enumerator.MoveNextAsync())
                {
                    action(enumerator.Current, index);
                    index++;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (enumerator)
            {
                long index = 0;

                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    action(enumerator.Current, index);
                    index++;
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="enumerable">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">An asynchronous action to perform for every single item in the collection</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> enumerable, Func<T, Task> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    await action(enumerator.Current).ConfigureAwait(false);
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (var enumerator = await enumerable.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    await action(enumerator.Current).ConfigureAwait(false);
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="enumerator">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">An asynchronous action to perform for every single item in the collection</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync<T>(this IAsyncEnumerator<T> enumerator, Func<T, Task> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    await action(enumerator.Current).ConfigureAwait(false);
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (enumerator)
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    await action(enumerator.Current).ConfigureAwait(false);
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="enumerable">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">An asynchronous action to perform for every single item in the collection, where the second argument is the index of an item</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> enumerable, Func<T, long, Task> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
            try
            {
                long index = 0;

                while (await enumerator.MoveNextAsync())
                {
                    await action(enumerator.Current, index).ConfigureAwait(false);
                    index++;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (var enumerator = await enumerable.GetAsyncEnumeratorAsync(cancellationToken).ConfigureAwait(false))
            {
                long index = 0;

                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    await action(enumerator.Current, index).ConfigureAwait(false);
                    index++;
                }
            }
#endif
        }

        /// <summary>
        /// Enumerates over all elements in the collection asynchronously
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="enumerator">The collection of elements which can be enumerated asynchronously</param>
        /// <param name="action">An asynchronous action to perform for every single item in the collection, where the second argument is the index of an item</param>
        /// <param name="cancellationToken">A cancellation token to stop enumerating</param>
        /// <returns>Returns a Task which does enumeration over elements in the collection</returns>
        public static async Task ForEachAsync<T>(this IAsyncEnumerator<T> enumerator, Func<T, long, Task> action, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP3_0
            try
            {
                long index = 0;

                while (await enumerator.MoveNextAsync())
                {
                    await action(enumerator.Current, index).ConfigureAwait(false);
                    index++;
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
#else
            using (enumerator)
            {
                long index = 0;

                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    await action(enumerator.Current, index).ConfigureAwait(false);
                    index++;
                }
            }
#endif
        }
    }
}
