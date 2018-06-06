using Logshark.Common.Exceptions;
using System;

namespace Logshark.Core.Exceptions
{
    [Serializable]
    public class ExtractionException : BaseLogsharkException
    {
        public ExtractionException()
        {
        }

        public ExtractionException(string message)
            : base(message)
        {
        }

        public ExtractionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}