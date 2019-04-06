namespace System.Collections.Async
{
    /// <summary>
    /// This exception is thrown when you call <see cref="ForEachAsyncExtensions.Break"/>.
    /// </summary>
    public sealed class ForEachAsyncCanceledException : OperationCanceledException { }
}
