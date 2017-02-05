using System.Collections.Async.Internals;
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

        object IEnumerator.Current => Current;

        public bool MoveNext() => false;

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken) => TaskEx.False;

        public void Reset() { }

        public Task ResetAsync(CancellationToken cancellationToken) => TaskEx.Completed;

        public void Dispose() { }
    }
}
