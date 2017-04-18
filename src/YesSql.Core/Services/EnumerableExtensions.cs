using System.Collections.Generic;
using System.Linq;

namespace System
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> PagesOf<T>(this IEnumerable<T> list, int pageSize)
        {
            if (!list.Any())
            {
                yield break;
            }

            var page = 0;
            var pages = (list.Count() - 1) / pageSize + 1;

            while (page < pages)
            {
                yield return list.Skip(page * pageSize).Take(pageSize);
                page++;
            }
        }
    }
}
