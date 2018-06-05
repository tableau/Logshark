using Logshark.Common.Exceptions;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Logshark.Core.Exceptions
{
    [Serializable]
    public class ServiceDependencyUnavailableException : BaseLogsharkException
    {
        public string DependencyName { get; protected set; }

        public ServiceDependencyUnavailableException(string dependencyName)
        {
            DependencyName = dependencyName;
        }

        public ServiceDependencyUnavailableException(string dependencyName, string message)
            : base(message)
        {
            DependencyName = dependencyName;
        }

        public ServiceDependencyUnavailableException(string dependencyName, string message, Exception inner)
            : base(message, inner)
        {
            DependencyName = dependencyName;
        }

        protected ServiceDependencyUnavailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            DependencyName = info.GetString("DependencyName");
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("DependencyName", DependencyName);
            base.GetObjectData(info, context);
        }
    }
}