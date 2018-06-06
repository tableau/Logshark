using Logshark.Common.Exceptions;
using System;

namespace Logshark.Core.Exceptions
{
    [Serializable]
    public class InsufficientDiskSpaceException : BaseLogsharkException
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