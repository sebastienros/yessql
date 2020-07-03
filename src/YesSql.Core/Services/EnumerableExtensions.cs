using System.Collections.Generic;
using System.Linq;
using YesSql;

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

        public static IEnumerable<KeyValuePair<string, IEnumerable<T>>> PagesOfByCollection<T>(this IEnumerable<T> list, int pageSize) where T : ICollectionName
        {
            if (!list.Any())
            {
                yield break;
            }

            var pagesByCollection = list.GroupBy(x => x.Collection);

            foreach (var group in pagesByCollection)
            {
                var page = 0;
                var pages = (group.Count() - 1) / pageSize + 1;

                while (page < pages)
                {
                    yield return new KeyValuePair<string, IEnumerable<T>>(group.Key, group.Skip(page * pageSize).Take(pageSize));
                    page++;
                }
            }
        }
    }
}
