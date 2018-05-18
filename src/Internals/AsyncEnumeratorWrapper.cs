using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async.Internals
{
    internal sealed class AsyncEnumeratorWrapper<T> : IAsyncEnumerator<T>
    {
        private IEnumerator<T> _enumerator;
        private readonly bool _runSynchronously;

        public AsyncEnumeratorWrapper(IEnumerator<T> enumerator, bool runSynchronously)
        {
            _enumerator = enumerator;
            _runSynchronously = runSynchronously;
        }

        public T Current => _enumerator.Current;

        object IAsyncEnumerator.Current => Current;

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            if (_runSynchronously) {
                var result = _enumerator.MoveNext();
                return result ? TaskEx.True : TaskEx.False;
            } else {
                return Task.Run(() => _enumerator.MoveNext(), cancellationToken);
            }
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
