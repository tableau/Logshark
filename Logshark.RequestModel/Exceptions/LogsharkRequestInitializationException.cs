using Logshark.Common.Exceptions;
using System;

namespace Logshark.RequestModel.Exceptions
{
    [Serializable]
    public class LogsharkRequestInitializationException : BaseLogsharkException
    {
        public LogsharkRequestInitializationException()
        {
        }

        public LogsharkRequestInitializationException(string message)
            : base(message)
        {
        }

        public LogsharkRequestInitializationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}