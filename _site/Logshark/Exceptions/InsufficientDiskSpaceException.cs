using System;

namespace Logshark.Exceptions
{
    public class InsufficientDiskSpaceException : Exception
    {
        public InsufficientDiskSpaceException()
        {
        }

        public InsufficientDiskSpaceException(string message)
            : base(message)
        {
        }

        public InsufficientDiskSpaceException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}