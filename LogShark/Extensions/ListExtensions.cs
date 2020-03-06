using System.Collections.Generic;

namespace LogShark.Extensions
{
    public static class ListExtensions
    {
        public static void MoveToFront<T>(this IList<T> list, int index)
        {
            if (index == 0) // Element is already at the front
            {
                return;
            }
            
            var item = list[index];
            list.RemoveAt(index);
            list.Insert(0, item);
        }
    }
}