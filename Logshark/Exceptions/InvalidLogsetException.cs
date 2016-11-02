using System;

namespace Logshark.Exceptions
{
    public class InvalidLogsetException : Exception
    {
        public InvalidLogsetException()
        {
        }

        public InvalidLogsetException(string message)
            : base(message)
        {
        }

        public InvalidLogsetException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}