namespace LogParsers.Base.Helpers
{
    /// <summary>
    /// Simple helper class intended for tracking line numbers.
    /// </summary>
    public sealed class LineCounter
    {
        /// <summary>
        /// Retrieve the current line number.
        /// </summary>
        public long CurrentValue
        {
            get;
            set;
        }

        public LineCounter(long offset = 0)
        {
            CurrentValue = offset;
        }

        /// <summary>
        /// Increment the current line number.
        /// </summary>
        public void Increment()
        {
            CurrentValue++;
        }

        /// <summary>
        /// Increments the current line number by fixed amount.
        /// </summary>
        /// <param name="num"></param>
        public void IncrementBy(int num)
        {
            CurrentValue += num;
        }
    }
}