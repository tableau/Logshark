using Logshark.Common.Exceptions;
using System;

namespace LogParsers.Base.Exceptions
{
    [Serializable]
    public class ParsingException : BaseLogsharkException
    {
        public ParsingException()
        {
        }

        public ParsingException(string message)
            : base(message)
        {
        }

        public ParsingException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}