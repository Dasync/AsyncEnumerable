using System.Collections.Generic;
using System.Threading;
using Dasync.Collections;

namespace Dasync.Collections.Internals
{
    internal sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => AsyncEnumerator<T>.Empty;

#if !NETSTANDARD2_1
        IAsyncEnumerator IAsyncEnumerable.GetAsyncEnumerator(CancellationToken cancellationToken)
            => AsyncEnumerator<T>.Empty;
#endif
    }
}
