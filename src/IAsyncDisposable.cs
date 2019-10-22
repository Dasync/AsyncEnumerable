#if !NETSTANDARD2_1 && !NETSTANDARD2_0 && !NET461
using System.Threading.Tasks;

namespace Dasync.Collections
{
    public interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
}
#endif
