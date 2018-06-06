using Logshark.Common.Exceptions;
using System;

namespace Logshark.Core.Exceptions
{
    [Serializable]
    public class PluginInitializationException : BaseLogsharkException
    {
        public PluginInitializationException()
        {
        }

        public PluginInitializationException(string message)
            : base(message)
        {
        }

        public PluginInitializationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}