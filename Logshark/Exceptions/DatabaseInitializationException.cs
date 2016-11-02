using System;

namespace Logshark.Exceptions
{
    public class DatabaseInitializationException : Exception
    {
        public DatabaseInitializationException()
        {
        }

        public DatabaseInitializationException(string message)
            : base(message)
        {
        }

        public DatabaseInitializationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}