using System;

namespace Logshark.Exceptions
{
    public class PublishingException : Exception
    {
        public PublishingException()
        {
        }

        public PublishingException(string message)
            : base(message)
        {
        }

        public PublishingException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}