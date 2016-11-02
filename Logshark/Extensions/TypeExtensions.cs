using System;
using System.Linq;

namespace Logshark.Extensions
{
    internal static class TypeExtensions
    {
        /// <summary>
        /// Indicates whether this type implements a given interface.
        /// </summary>
        /// <param name="type">This type.</param>
        /// <param name="interfaceType">The interface to check if this type implmenets.</param>
        /// <returns>True if type implements interfaceType.</returns>
        public static bool Implements(this Type type, Type interfaceType)
        {
            return type.GetInterfaces().Contains(interfaceType);
        }
    }
}