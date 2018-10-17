using System;

namespace Tableau.ExtractApi.Exceptions
{
    [Serializable]
    public abstract class BaseExtractException : Exception
    {
        protected BaseExtractException()
        {
        }

        protected BaseExtractException(string message)
            : base(message)
        {
        }

        protected BaseExtractException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}