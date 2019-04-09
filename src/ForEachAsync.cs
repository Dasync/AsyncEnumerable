namespace System.Collections.Async
{
    /// <summary>
    /// Class to provide access to static <see cref="Break"/> method.
    /// </summary>
    public static class ForEachAsync
    {
        /// <summary>
        /// Stops ForEachAsync iteration (similar to 'break' statement)
        /// </summary>
        /// <exception cref="ForEachAsyncCanceledException">Always throws this exception to stop the ForEachAsync iteration</exception>
        public static void Break()
        {
            throw new ForEachAsyncCanceledException();
        }
    }
}
