using System.Collections.Generic;
using System.Threading;
using Dasync.Collections;

namespace Dasync.Collections.Internals
{
    internal sealed class AsyncEnumerableWrapper<T> : IAsyncEnumerable, IAsyncEnumerable<T>
    {
        private readonly IEnumerable<T> _enumerable;
        private readonly bool _runSynchronously;

        public AsyncEnumerableWrapper(IEnumerable<T> enumerable, bool runSynchronously)
        {
            _enumerable = enumerable;
            _runSynchronously = runSynchronously;
        }

        IAsyncEnumerator IAsyncEnumerable.GetAsyncEnumerator(CancellationToken cancellationToken)
            => new AsyncEnumeratorWrapper<T>(_enumerable.GetEnumerator(), _runSynchronously)
                { MasterCancellationToken = cancellationToken };

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new AsyncEnumeratorWrapper<T>(_enumerable.GetEnumerator(), _runSynchronously)
                { MasterCancellationToken = cancellationToken };
    }
}
