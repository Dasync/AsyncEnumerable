using System.Collections.Generic;

namespace System.Collections.Async.Internals
{
#if !NETCOREAPP3_0
    internal sealed class EnumerableAdapter : IEnumerable
    {
        private readonly IAsyncEnumerable _asyncEnumerable;

        public EnumerableAdapter(IAsyncEnumerable asyncEnumerable)
        {
            _asyncEnumerable = asyncEnumerable;
        }

        public IEnumerator GetEnumerator() =>
            new EnumeratorAdapter(_asyncEnumerable.GetAsyncEnumerator());
    }
#endif

    internal sealed class EnumerableAdapter<T> : IEnumerable, IEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _asyncEnumerable;

        public EnumerableAdapter(IAsyncEnumerable<T> asyncEnumerable)
        {
            _asyncEnumerable = asyncEnumerable;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator() =>
            new EnumeratorAdapter<T>(_asyncEnumerable.GetAsyncEnumerator());
    }
}