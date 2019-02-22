using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async.Internals
{
    internal sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
#if NETCOREAPP3_0
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => AsyncEnumerator<T>.Empty;
#else
        private static readonly Task<IAsyncEnumerator<T>> CompletedGetAsyncEnumeratorAsyncTask
            = Task.FromResult(AsyncEnumerator<T>.Empty);

        private static readonly Task<IAsyncEnumerator> CompletedGetAsyncEnumeratorAsyncNonGenericTask
            = Task.FromResult((IAsyncEnumerator)AsyncEnumerator<T>.Empty);

        public Task<IAsyncEnumerator<T>> GetAsyncEnumeratorAsync(CancellationToken cancellationToken)
            => CompletedGetAsyncEnumeratorAsyncTask;

        Task<IAsyncEnumerator> IAsyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken cancellationToken)
            => CompletedGetAsyncEnumeratorAsyncNonGenericTask;
#endif
    }
}
