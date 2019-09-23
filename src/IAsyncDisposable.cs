#if !NETSTANDARD2_1
using System.Threading.Tasks;

namespace Dasync.Collections
{
    public interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
}
#endif
