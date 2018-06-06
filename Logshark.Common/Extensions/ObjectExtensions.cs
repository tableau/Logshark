using System;

namespace Logshark.Common.Extensions
{
    public static class ObjectExtensions
    {
        public static TResult Map<T, TResult>(this T obj, Func<T, TResult> transform)
        {
            return transform(obj);
        }
    }
}