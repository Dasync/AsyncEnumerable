using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    internal sealed class AsyncEnumerableWrapper<T> : IAsyncEnumerable<T>
    {
        private IEnumerable<T> _enumerable;
        private bool _runSynchronously;

        public AsyncEnumerableWrapper(IEnumerable<T> enumerable, bool runSynchronously)
        {
            _enumerable = enumerable;
            _runSynchronously = runSynchronously;
        }

        public Task<IAsyncEnumerator<T>> GetAsyncEnumeratorAsync(CancellationToken cancellationToken = default(CancellationToken)) => Task.FromResult(CreateAsyncEnumerator());

        Task<IAsyncEnumerator> IAsyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken cancellationToken) => Task.FromResult<IAsyncEnumerator>(CreateAsyncEnumerator());

        public IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _enumerable.GetEnumerator();

        private IAsyncEnumerator<T> CreateAsyncEnumerator() => new AsyncEnumeratorWrapper<T>(_enumerable.GetEnumerator(), _runSynchronously);
    }
}
