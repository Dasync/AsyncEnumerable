using System.Threading.Tasks;

namespace System.Collections.Async.Internals
{
    internal static class TaskEx
    {
        public static readonly Task<bool> True = Task.FromResult(true);
        public static readonly Task<bool> False = Task.FromResult(false);
        public static readonly Task Completed = True;
    }
}