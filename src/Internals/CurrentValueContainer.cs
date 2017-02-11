namespace System.Collections.Async.Internals
{
    /// <summary>
    /// Internal base type for <see cref="AsyncEnumerator{T}"/> and <see cref="AsyncEnumeratorWithState{TItem, TState}"/>
    /// </summary>
    public abstract class CurrentValueContainer<T> : AsyncEnumerator
    {
        internal T CurrentValue;
    }
}
