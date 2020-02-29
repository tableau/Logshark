using System;

namespace LogShark.Exceptions
{
    /// <summary>
    /// Exception used to report problems occurred while publishing workbooks
    /// </summary>
    public class WorkbookPublishingException : Exception
    {
        public WorkbookPublishingException()
        {
        }

        public WorkbookPublishingException(string message) : base(message)
        {
        }

        public WorkbookPublishingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}