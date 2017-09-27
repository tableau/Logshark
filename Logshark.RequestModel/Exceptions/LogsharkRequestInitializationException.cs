using System;

namespace Logshark.RequestModel.Exceptions
{
    public class LogsharkRequestInitializationException : Exception
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