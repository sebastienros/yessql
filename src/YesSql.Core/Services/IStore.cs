using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using YesSql.Core.Data;
using YesSql.Core.Indexes;
using YesSql.Core.Sql;

namespace YesSql.Core.Services
{
    public interface IStore : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="ISession"/> to communicate with the <see cref="IStore"/>
        /// </summary>
        ISession CreateSession();

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
        Task ExecuteMigrationAsync(Action<SchemaBuilder> migration, bool throwException = true);
        IEnumerable<IndexDescriptor> Describe(Type target);
    }
}
