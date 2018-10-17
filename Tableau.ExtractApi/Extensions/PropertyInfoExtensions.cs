using System.Linq;
using System.Reflection;
using Tableau.ExtractApi.DataAttributes;

namespace Tableau.ExtractApi.Extensions
{
    internal static class PropertyInfoExtensions
    {
        public static bool IsPersistable(this PropertyInfo property)
        {
            return HasPublicGetter(property) && !IsAnnotatedWithIgnoreAttribute(property);
        }

        private static bool HasPublicGetter(PropertyInfo property)
        {
            return property.GetGetMethod() != null;
        }

        private static bool IsAnnotatedWithIgnoreAttribute(PropertyInfo property)
        {
            return property.GetCustomAttributes().Any(attribute => attribute is ExtractIgnoreAttribute);
        }
    }
}