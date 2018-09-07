using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YesSql.Indexes;

namespace YesSql
{
    public static class IndexProviderRegisterExtensions
    {
        public static IStore RegisterIndexes<T>(this IStore store) where T : IIndexProvider
        {
            return store.RegisterIndexes(typeof(T));
        }

        public static IStore RegisterIndexes(this IStore store, IIndexProvider indexProvider)
        {
            if (indexProvider != null)
            {
                return store.RegisterIndexes(new[] { indexProvider });
            }

            return store.RegisterIndexes(new IIndexProvider[0]);
        }

        public static IStore RegisterIndexes(this IStore store, Type type)
        {
            var index = Activator.CreateInstance(type) as IIndexProvider;

            return store.RegisterIndexes(index);
        }

        public static IStore RegisterIndexes(this IStore store, IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                store.RegisterIndexes(type);
            }

            return store.RegisterIndexes(new IIndexProvider[0]);
        }

        public static IStore RegisterIndexes(this IStore store, Assembly assembly)
        {
            var exportedTypes = assembly.GetExportedTypes();
            var indexes = exportedTypes.Where(x => typeof(IIndexProvider).IsAssignableFrom(x));
            return store.RegisterIndexes(indexes);
        }

    }
}
