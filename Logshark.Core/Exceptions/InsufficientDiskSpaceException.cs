using System;

namespace Logshark.Core.Exceptions
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