using Logshark.Common.Exceptions;
using System;

namespace Logshark.Core.Exceptions
{
    [Serializable]
    public class MetadataWriterException : BaseLogsharkException
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