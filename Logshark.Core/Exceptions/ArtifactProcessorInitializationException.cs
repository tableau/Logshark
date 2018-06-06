using Logshark.Common.Exceptions;
using System;

namespace Logshark.Core.Exceptions
{
    [Serializable]
    public class ArtifactProcessorInitializationException : BaseLogsharkException
    {
        public ArtifactProcessorInitializationException()
        {
        }

        public ArtifactProcessorInitializationException(string message)
            : base(message)
        {
        }

        public ArtifactProcessorInitializationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}