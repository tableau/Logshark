using System;

namespace LogShark.Exceptions
{
    /// <summary>
    /// Exception used to report problems with LogShark configuration
    /// </summary>
    public class LogSharkConfigurationException : Exception
    {
        public LogSharkConfigurationException()
        {
        }

        public LogSharkConfigurationException(string message) : base(message)
        {
        }

        public LogSharkConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}