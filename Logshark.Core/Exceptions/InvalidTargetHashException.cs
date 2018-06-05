using System;

namespace Logshark.Core.Exceptions
{
    [Serializable]
    public class InvalidTargetHashException : InvalidLogsetException
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