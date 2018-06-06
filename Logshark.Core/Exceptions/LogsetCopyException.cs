using Logshark.Common.Exceptions;
using System;

namespace Logshark.Core.Exceptions
{
    [Serializable]
    public class LogsetCopyException : BaseLogsharkException
    {
        public LogsetCopyException()
        {
        }

        public LogsetCopyException(string message)
            : base(message)
        {
        }

        public LogsetCopyException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}