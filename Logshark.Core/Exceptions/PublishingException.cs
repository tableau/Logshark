using System;

namespace Logshark.Core.Exceptions
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