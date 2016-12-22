using System;

namespace Logshark.Exceptions
{
    public class ProcessingUserCollisionException : ProcessingException
    {
        public ProcessingUserCollisionException()
        {
        }

        public ProcessingUserCollisionException(string message)
            : base(message)
        {
        }

        public ProcessingUserCollisionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}