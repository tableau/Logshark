using System;

namespace Tableau.ExtractApi.Exceptions
{
    [Serializable]
    public class ExtractInsertionException : BaseExtractException
    {
        public ExtractInsertionException()
        {
        }

        public ExtractInsertionException(string message)
            : base(message)
        {
        }

        public ExtractInsertionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}