using System;

namespace LogShark.Exceptions
{
    /// <summary>
    /// This should be thrown if we detected a condition likely caused by a code/developer error.
    /// </summary>
    public class LogSharkProgramLogicException : Exception
    {
        public LogSharkProgramLogicException()
        {
        }

        public LogSharkProgramLogicException(string message) : base(message)
        {
        }

        public LogSharkProgramLogicException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}