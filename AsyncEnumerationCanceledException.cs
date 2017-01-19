namespace System.Collections.Async
{
    /// <summary>
    /// This exception is thrown when you call <see cref="AsyncEnumerator{T}.Yield.Break"/>
    /// or when the enumerator is disposed before reaching the end of enumeration.
    /// </summary>
    public sealed class AsyncEnumerationCanceledException : OperationCanceledException { }
}
