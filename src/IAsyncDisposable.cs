using System.Threading.Tasks;

namespace System.Collections.Async
{
#if !NETCOREAPP3_0
    public interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
#endif
}