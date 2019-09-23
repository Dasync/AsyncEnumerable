using System;
using System.Threading.Tasks;

namespace Dasync.Collections.Internals
{
    internal static class TaskEx
    {
        public static readonly Task<bool> True = Task.FromResult(true);
        public static readonly Task<bool> False = Task.FromResult(false);
        public static readonly Task Completed =
#if NETFX4_5 || NETFX4_5_2
            True;
#else
            Task.CompletedTask;
#endif

        public static Task<T> FromException<T>(Exception ex)
        {
#if NETFX4_5 || NETFX4_5_2
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(ex);
            return tcs.Task;
#else
            return Task.FromException<T>(ex);
#endif
        }
    }
}