using System;

namespace Dasync.Collections
{
    /// <summary>
    /// This exception is thrown when you call <see cref="ForEachAsync.Break"/>.
    /// </summary>
    public sealed class ForEachAsyncBreakException : OperationCanceledException { }
}
