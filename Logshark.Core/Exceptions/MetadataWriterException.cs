using System;

namespace Logshark.Core.Exceptions
{
    public class MetadataWriterException : Exception
    {
        public MetadataWriterException()
        {
        }

        public MetadataWriterException(string message)
            : base(message)
        {
        }

        public MetadataWriterException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}