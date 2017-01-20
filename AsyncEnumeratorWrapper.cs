using System.Collections.Async.Internals;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async
{
    internal sealed class AsyncEnumeratorWrapper<T> : IAsyncEnumerator<T>
    {
        private IEnumerator<T> _enumerator;
        private bool _runSynchronously;

        public AsyncEnumeratorWrapper(IEnumerator<T> enumerator, bool runSynchronously)
        {
            _enumerator = enumerator;
            _runSynchronously = runSynchronously;
        }

        public T Current => _enumerator.Current;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_runSynchronously) {
                return _enumerator.MoveNext();
            } else {
                return MoveNextAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_runSynchronously) {
                var result = _enumerator.MoveNext();
                return result ? TaskEx.True : TaskEx.False;
            } else {
                return Task.Run(() => _enumerator.MoveNext(), cancellationToken);
            }
        }

        public void Reset()
        {
            if (_runSynchronously) {
                _enumerator.Reset();
            } else {
                ResetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        public Task ResetAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_runSynchronously) {
                _enumerator.Reset();
                return TaskEx.Completed;
            } else {
                return Task.Run(() => _enumerator.Reset(), cancellationToken);
            }
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
