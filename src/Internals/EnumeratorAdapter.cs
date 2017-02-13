using System.Collections.Generic;

namespace System.Collections.Async.Internals
{
    internal sealed class EnumeratorAdapter : IEnumerator
    {
        private readonly IAsyncEnumerator _asyncEnumerator;

        public EnumeratorAdapter(IAsyncEnumerator asyncEnumerator)
        {
            _asyncEnumerator = asyncEnumerator;
        }

        public object Current => _asyncEnumerator.Current;

        public bool MoveNext() => _asyncEnumerator.MoveNextAsync().GetAwaiter().GetResult();

        public void Reset() => throw new NotSupportedException("The IEnumerator.Reset() method is obsolete. Create a new enumerator instead.");

        public void Dispose() => _asyncEnumerator.Dispose();
    }

    internal sealed class EnumeratorAdapter<T> : IEnumerator, IEnumerator<T>
    {
        private readonly IAsyncEnumerator<T> _asyncEnumerator;

        public EnumeratorAdapter(IAsyncEnumerator<T> asyncEnumerator)
        {
            _asyncEnumerator = asyncEnumerator;
        }

        public T Current => _asyncEnumerator.Current;

        object IEnumerator.Current => Current;

        public bool MoveNext() => _asyncEnumerator.MoveNextAsync().GetAwaiter().GetResult();

        public void Reset() => throw new NotSupportedException("The IEnumerator.Reset() method is obsolete. Create a new enumerator instead.");

        public void Dispose() => _asyncEnumerator.Dispose();
    }
}