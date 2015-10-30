using System;
using System.Collections.Generic;
using System.Reflection;
using YesSql.Core.Data;
using YesSql.Core.Indexes;

namespace YesSql.Core.Services
{
    public interface IStore : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="ISession"/> to communicate with the <see cref="IStore"/>
        /// </summary>
        ISession CreateSession(bool trackChanges = true);

        /// <summary>
        /// Registers an index using an <see cref="IIndexProvider"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        IStore RegisterIndexes<T>() where T : IIndexProvider;
        IStore RegisterIndexes(Type type);
        IStore RegisterIndexes(IEnumerable<Type> types);
        IStore RegisterIndexes(Assembly assembly);

        IIdAccessor GetIdAccessor(Type tContainer, string name);

        IEnumerable<IndexDescriptor> Describe(Type target);
    }
}
