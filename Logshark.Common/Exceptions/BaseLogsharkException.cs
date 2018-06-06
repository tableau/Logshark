using System;
using System.Runtime.Serialization;

namespace Logshark.Common.Exceptions
{
    [Serializable]
    public abstract class BaseLogsharkException : Exception
    {
        protected BaseLogsharkException()
        {
        }

        protected BaseLogsharkException(string message)
            : base(message)
        {
        }

        protected BaseLogsharkException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected BaseLogsharkException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}