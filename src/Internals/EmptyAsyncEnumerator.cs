using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async.Internals
{
    internal sealed class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        public T Current
        {
            get
            {
                throw new InvalidOperationException("The enumerator has reached the end of the collection");
            }
        }

        object IAsyncEnumerator.Current => Current;

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken) => TaskEx.False;

        public void Dispose() { }
    }
}
