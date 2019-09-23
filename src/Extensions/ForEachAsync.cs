namespace Dasync.Collections
{
    /// <summary>
    /// Class to provide access to static <see cref="Break"/> method.
    /// </summary>
    public static class ForEachAsync
    {
        /// <summary>
        /// Stops ForEachAsync iteration (similar to 'break' statement)
        /// </summary>
        /// <exception cref="ForEachAsyncBreakException">Always throws this exception to stop the ForEachAsync iteration</exception>
        public static void Break()
        {
            throw new ForEachAsyncBreakException();
        }
    }
}
