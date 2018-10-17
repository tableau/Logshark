using System;

namespace Tableau.ExtractApi.Exceptions
{
    [Serializable]
    public class ExtractTableCreationException : BaseExtractException
    {
        public ExtractTableCreationException()
        {
        }

        public ExtractTableCreationException(string message)
            : base(message)
        {
        }

        public ExtractTableCreationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}