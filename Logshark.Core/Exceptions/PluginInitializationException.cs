using System;

namespace Logshark.Core.Exceptions
{
    public class PluginInitializationException : Exception
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