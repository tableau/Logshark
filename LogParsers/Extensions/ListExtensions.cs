using System.Collections.Generic;

namespace LogParsers.Extensions
{
    /// <summary>
    /// IList extension to move an item to the front of the list.
    /// </summary>
    internal static class ListExtensions
    {
        public static void MoveToFront<T>(this IList<T> list, int index)
        {
            T item = list[index];
            list.RemoveAt(index);
            list.Insert(0, item);
        }
    }
}
