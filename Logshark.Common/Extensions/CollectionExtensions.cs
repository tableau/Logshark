using System.Collections.Generic;

namespace Logshark.Common.Extensions
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> rangeToAdd)
        {
            List<T> list = collection as List<T>;
            if (list == null)
            {
                foreach (T item in rangeToAdd)
                {
                    collection.Add(item);
                }
            }
            else
            {
                // List.AddRange has performance benefits over the foreach above.
                list.AddRange(rangeToAdd);
            }
        }
    }
}