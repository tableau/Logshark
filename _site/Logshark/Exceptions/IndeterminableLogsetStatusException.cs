using System;

namespace Logshark.Exceptions
{
    public class IndeterminableLogsetStatusException : ProcessingException
    {
        public IndeterminableLogsetStatusException()
        {
        }

        public IndeterminableLogsetStatusException(string message)
            : base(message)
        {
        }

        public IndeterminableLogsetStatusException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}