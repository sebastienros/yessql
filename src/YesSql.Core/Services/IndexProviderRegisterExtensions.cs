using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YesSql.Indexes;

namespace YesSql
{
    public static class IndexProviderRegisterExtensions
    {
        public static IStore RegisterIndexes<T>(this IStore store, string collection = null) where T : IIndexProvider
        {
            return store.RegisterIndexes(typeof(T), collection);
        }

        public static IStore RegisterIndexes(this IStore store, IIndexProvider indexProvider, string collection = null)
        {
            if (indexProvider != null)
            {
                return store.RegisterIndexes(new[] { indexProvider }, collection);
            }

            return store.RegisterIndexes(new IIndexProvider[0], collection);
        }

        public static IStore RegisterIndexes(this IStore store, Type type, string collection = null)
        {
            var index = Activator.CreateInstance(type) as IIndexProvider;

            return store.RegisterIndexes(index, collection);
        }

        public static IStore RegisterIndexes(this IStore store, IEnumerable<Type> types, string collection = null)
        {
            foreach (var type in types)
            {
                store.RegisterIndexes(type, collection);
            }

            return store.RegisterIndexes(new IIndexProvider[0], collection);
        }

        public static IStore RegisterIndexes(this IStore store, Assembly assembly, string collection = null)
        {
            var exportedTypes = assembly.GetExportedTypes();
            var indexes = exportedTypes.Where(x => typeof(IIndexProvider).IsAssignableFrom(x));
            return store.RegisterIndexes(indexes, collection);
        }

    }
}
