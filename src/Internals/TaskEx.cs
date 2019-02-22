using System.Threading.Tasks;

namespace System.Collections.Async.Internals
{
    internal static class TaskEx
    {
        public static readonly Task<bool> True = Task.FromResult(true);
        public static readonly Task<bool> False = Task.FromResult(false);
        public static readonly Task Completed = True;

        public static Task<T> FromException<T>(Exception ex)
        {
#if NETFX4_5
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(ex);
            return tcs.Task;
#else
            return Task.FromException<T>(ex);
#endif
        }
    }
}