using System;

namespace LogShark.HostExtensions
{
    public class HostExtensionLoadException : Exception
    {
        public HostExtensionLoadException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }
}
