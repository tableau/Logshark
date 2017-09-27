using System;

namespace Logshark.Core.Exceptions
{
    public class InvalidTargetHashException : ProcessingException
    {
        public InvalidTargetHashException()
        {
        }

        public InvalidTargetHashException(string message)
            : base(message)
        {
        }

        public InvalidTargetHashException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}