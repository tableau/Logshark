using System;

namespace Tableau.ExtractApi.Exceptions
{
    [Serializable]
    public class ExtractInitializationException : BaseExtractException
    {
        public ExtractInitializationException()
        {
        }

        public ExtractInitializationException(string message)
            : base(message)
        {
        }

        public ExtractInitializationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}