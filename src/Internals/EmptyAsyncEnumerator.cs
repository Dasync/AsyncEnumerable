using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Async.Internals
{
    internal sealed class EmptyAsyncEnumerator<T> : IAsyncEnumerator, IAsyncEnumerator<T>
    {
        public T Current
        {
            get
            {
                throw new InvalidOperationException("The enumerator has reached the end of the collection");
            }
        }

        object IAsyncEnumerator.Current => Current;

        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(false);

        public ValueTask DisposeAsync() => new ValueTask();

        public void Dispose() { }
    }
}
