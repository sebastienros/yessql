using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using YesSql.Core.Data;
using YesSql.Core.Indexes;

namespace YesSql.Core.Services
{
    public interface IStore : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="ISession"/> to communicate with the <see cref="IStore"/> with
        /// the specified <see cref="IsolationLevel"/>.
        /// </summary>
        ISession CreateSession(IsolationLevel isolationLevel);

        /// <summary>
        /// Registers an index using an <see cref="IIndexProvider"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        IStore RegisterIndexes<T>() where T : IIndexProvider;
        IStore RegisterIndexes(Type type);
        IStore RegisterIndexes(IEnumerable<Type> types);
        IStore RegisterIndexes(Assembly assembly);
        Configuration Configuration { get; set; }
        Task InitializeAsync();
        IIdAccessor<int> GetIdAccessor(Type tContainer, string name);
        int GetNextId();
        IEnumerable<IndexDescriptor> Describe(Type target);
    }

    public static class IStoreExtensions
    {
        /// <summary>
        /// Creates a new <see cref="ISession"/> to communicate with the <see cref="IStore"/> with
        /// the default <see cref="IsolationLevel"/>.
        /// </summary>
        public static ISession CreateSession(this IStore store)
        {
            return store.CreateSession(store.Configuration.IsolationLevel);
        }
    }
}
