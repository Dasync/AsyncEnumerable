using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async.Internals
{
    internal sealed class AsyncEnumeratorWrapper<T> : IAsyncEnumerator, IAsyncEnumerator<T>
    {
        private IEnumerator<T> _enumerator;
        private readonly bool _runSynchronously;

        public AsyncEnumeratorWrapper(IEnumerator<T> enumerator, bool runSynchronously)
        {
            _enumerator = enumerator;
            _runSynchronously = runSynchronously;
        }

#if NETCOREAPP3_0
        internal CancellationToken MasterCancellationToken;
#endif

        public T Current => _enumerator.Current;

        object IAsyncEnumerator.Current => Current;

#if NETCOREAPP3_0
        public ValueTask<bool> MoveNextAsync()
        {
            if (_runSynchronously)
            {
                try
                {
                    return new ValueTask<bool>(_enumerator.MoveNext());
                }
                catch (Exception ex)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    tcs.SetException(ex);
                    return new ValueTask<bool>(tcs.Task);
                }
            }
            else
            {
                return new ValueTask<bool>(Task.Run(() => _enumerator.MoveNext(), MasterCancellationToken));
            }
        }
#else
        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            if (_runSynchronously) {
                var result = _enumerator.MoveNext();
                return result ? TaskEx.True : TaskEx.False;
            } else {
                return Task.Run(() => _enumerator.MoveNext(), cancellationToken);
            }
        }
#endif

        public void Dispose()
        {
            _enumerator.Dispose();
        }

#if NETCOREAPP3_0
        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask();
        }
#else
        public Task DisposeAsync()
        {
            Dispose();
            return TaskEx.Completed;
        }
#endif
    }
}
