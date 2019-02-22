﻿using System.Collections.Generic;

namespace System.Collections.Async.Internals
{
    internal sealed class EnumerableAdapter : IEnumerable
    {
        private readonly IAsyncEnumerable _asyncEnumerable;

        public EnumerableAdapter(IAsyncEnumerable asyncEnumerable)
        {
            _asyncEnumerable = asyncEnumerable;
        }

        public IEnumerator GetEnumerator() =>
            new EnumeratorAdapter(
#if NETCOREAPP3_0
                _asyncEnumerable.GetAsyncEnumerator()
#else
                _asyncEnumerable.GetAsyncEnumeratorAsync().GetAwaiter().GetResult()
#endif
            );
    }

    internal sealed class EnumerableAdapter<T> : IEnumerable, IEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _asyncEnumerable;

        public EnumerableAdapter(IAsyncEnumerable<T> asyncEnumerable)
        {
            _asyncEnumerable = asyncEnumerable;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<T> GetEnumerator() =>
            new EnumeratorAdapter<T>(
#if NETCOREAPP3_0
                _asyncEnumerable.GetAsyncEnumerator()
#else
                _asyncEnumerable.GetAsyncEnumeratorAsync().GetAwaiter().GetResult()
#endif
            );
    }
}