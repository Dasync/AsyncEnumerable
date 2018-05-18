using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async.Internals
{
    internal sealed class AsyncEnumerableWrapper<T> : IAsyncEnumerable<T>
    {
        private IEnumerable<T> _enumerable;
        private readonly bool _runSynchronously;

        public AsyncEnumerableWrapper(IEnumerable<T> enumerable, bool runSynchronously)
        {
            _enumerable = enumerable;
            _runSynchronously = runSynchronously;
        }

        public Task<IAsyncEnumerator<T>> GetAsyncEnumeratorAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateAsyncEnumerator());

        Task<IAsyncEnumerator> IAsyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken cancellationToken) => Task.FromResult<IAsyncEnumerator>(CreateAsyncEnumerator());

        private IAsyncEnumerator<T> CreateAsyncEnumerator() => new AsyncEnumeratorWrapper<T>(_enumerable.GetEnumerator(), _runSynchronously);
    }
}
