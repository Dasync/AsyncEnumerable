using System.Collections;
using System.Collections.Generic;
using Dasync.Collections;

namespace Dasync.Collections.Internals
{
#if !NETSTANDARD2_1 && !NETSTANDARD2_0 && !NET461
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