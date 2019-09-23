using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;

namespace Dasync.Collections.Internals
{
    internal sealed class AsyncEnumeratorWrapper<T> : IAsyncEnumerator, IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private readonly bool _runSynchronously;

        public AsyncEnumeratorWrapper(IEnumerator<T> enumerator, bool runSynchronously)
        {
            _enumerator = enumerator;
            _runSynchronously = runSynchronously;
        }

        internal CancellationToken MasterCancellationToken;

        public T Current => _enumerator.Current;

        object IAsyncEnumerator.Current => Current;

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

        public void Dispose()
        {
            _enumerator.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask();
        }
    }
}
