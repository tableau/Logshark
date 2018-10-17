using System;

namespace Tableau.ExtractApi.Exceptions
{
    [Serializable]
    public class ExtractTableLoadException : BaseExtractException
    {
        public ExtractTableLoadException()
        {
        }

        public ExtractTableLoadException(string message)
            : base(message)
        {
        }

        public ExtractTableLoadException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}