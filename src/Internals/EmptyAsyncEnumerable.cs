using System.Collections.Generic;
using System.Threading;

namespace System.Collections.Async.Internals
{
    internal sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => AsyncEnumerator<T>.Empty;

#if !NETCOREAPP3_0
        IAsyncEnumerator IAsyncEnumerable.GetAsyncEnumerator(CancellationToken cancellationToken)
            => AsyncEnumerator<T>.Empty;
#endif
    }
}
