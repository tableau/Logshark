using Logshark.Common.Exceptions;
using System;

namespace Logshark.Core.Exceptions
{
    [Serializable]
    public class PublishingException : BaseLogsharkException
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