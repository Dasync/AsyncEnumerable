namespace System.Collections.Async
{
    /// <summary>
    /// This exception is thrown when you call <see cref="AsyncEnumerable{T}.Break"/>.
    /// </summary>
    public sealed class ForEachAsyncCanceledException : OperationCanceledException { }
}
