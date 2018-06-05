using Logshark.Common.Exceptions;
using System;

namespace Logshark.Core.Exceptions
{
    [Serializable]
    public class ProcessingException : BaseLogsharkException
    {
        public ProcessingException()
        {
        }

        public ProcessingException(string message)
            : base(message)
        {
        }

        public ProcessingException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}