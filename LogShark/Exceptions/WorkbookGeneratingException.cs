using System;

namespace LogShark.Exceptions
{
    /// <summary>
    /// Exception used to report problems occurred while generating workbooks
    /// </summary>
    public class WorkbookGeneratingException : Exception
    {
        public WorkbookGeneratingException()
        {
        }

        public WorkbookGeneratingException(string message) : base(message)
        {
        }

        public WorkbookGeneratingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}