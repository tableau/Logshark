using Logshark.Common.Exceptions;
using System;

namespace Logshark.Core.Exceptions
{
    [Serializable]
    public class InvalidLogsetException : BaseLogsharkException
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