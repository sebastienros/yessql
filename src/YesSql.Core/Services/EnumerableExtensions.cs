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
        public static IEnumerable<IEnumerable<T>> SplitPagesBy<T>(this IEnumerable<IEnumerable<T>> pages, Func<T, string> stringForSplit)
        {
            if (!pages.Any())
            {
                yield break;
            }
            
            foreach (var list in pages)
            {
                if (stringForSplit == null)
                {
                    yield return list;
                    continue;
                }
                var resultList = new List<T>();
                string previousGroupString = null;
                foreach (var elem in list)
                {
                    var groupString = stringForSplit(elem);
                    if (previousGroupString != null && previousGroupString != groupString)
                    {
                        yield return resultList;
                        resultList = new List<T>(new T[] { elem });
                    }
                    else
                    {
                        resultList.Add(elem);
                    }
                    previousGroupString = groupString;
                }
                if (resultList.Any())
                {
                    yield return resultList;
                }
            }
        }
    }
}
