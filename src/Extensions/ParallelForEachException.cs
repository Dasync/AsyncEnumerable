using System;
using System.Collections.Generic;

namespace Dasync.Collections
{
    /// <summary>
    /// Used in ParallelForEachAsync&lt;T&gt; extension method
    /// </summary>
    public class ParallelForEachException : AggregateException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ParallelForEachException(IEnumerable<Exception> innerExceptions)
            : base(innerExceptions)
        {
        }
    }
}
