using System;

namespace Logshark.Exceptions
{
    public class ExtractionException : Exception
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