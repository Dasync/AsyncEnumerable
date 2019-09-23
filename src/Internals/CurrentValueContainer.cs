namespace Dasync.Collections.Internals
{
    /// <summary>
    /// Internal base type for <see cref="AsyncEnumerator{T}"/> and <see cref="AsyncEnumeratorWithState{TItem, TState}"/>
    /// </summary>
    public abstract class CurrentValueContainer<T> : AsyncEnumerator
    {
        private T _currentValue;

        internal T CurrentValue
        {
            get
            {
                return _currentValue;
            }
            set
            {
                _currentValue = value;
                HasCurrentValue = true;
            }
        }

        internal bool HasCurrentValue
        {
            get;
            private set;
        }
    }
}
