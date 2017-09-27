using System;

namespace Logshark.Core.Exceptions
{
    public class ArtifactProcessorInitializationException : Exception
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
