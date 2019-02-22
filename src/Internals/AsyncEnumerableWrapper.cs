using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async.Internals
{
    internal sealed class AsyncEnumerableWrapper<T> : IAsyncEnumerable, IAsyncEnumerable<T>
    {
        private IEnumerable<T> _enumerable;
        private readonly bool _runSynchronously;

        public AsyncEnumerableWrapper(IEnumerable<T> enumerable, bool runSynchronously)
        {
            _enumerable = enumerable;
            _runSynchronously = runSynchronously;
        }

#if NETCOREAPP3_0

        IAsyncEnumerator IAsyncEnumerable.GetAsyncEnumerator(CancellationToken cancellationToken)
            => new AsyncEnumeratorWrapper<T>(_enumerable.GetEnumerator(), _runSynchronously)
                { MasterCancellationToken = cancellationToken };

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new AsyncEnumeratorWrapper<T>(_enumerable.GetEnumerator(), _runSynchronously)
                { MasterCancellationToken = cancellationToken };
#else
        public Task<IAsyncEnumerator<T>> GetAsyncEnumeratorAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return Task.FromResult(CreateAsyncEnumerator());
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<IAsyncEnumerator<T>>(ex);
            }
        }

        Task<IAsyncEnumerator> IAsyncEnumerable.GetAsyncEnumeratorAsync(CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult<IAsyncEnumerator>(CreateAsyncEnumerator());
            }
            catch (Exception ex)
            {
                return TaskEx.FromException<IAsyncEnumerator>(ex);
            }
        }

        private IAsyncEnumerator<T> CreateAsyncEnumerator() => new AsyncEnumeratorWrapper<T>(_enumerable.GetEnumerator(), _runSynchronously);
#endif
    }
}
