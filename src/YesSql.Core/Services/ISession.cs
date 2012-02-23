using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using YesSql.Core.Data.Models;
using YesSql.Core.Indexes;
using YesSql.Core.Query;

namespace YesSql.Core.Services
{
    /// <summary>
    /// Represents a connection to the document store.
    /// </summary>
    public interface ISession : IDisposable
    {
        /// <summary>
        /// Saves a document or its modifications to the store.
        /// </summary>
        void Save(Document document);

        /// <summary>
        /// Saves an object or its modifications to the store, and updates
        /// the corresponding indexes.
        /// </summary>
        void Save(object obj);

        /// <summary>
        /// Deletes a document from the store.
        /// </summary>
        void Delete(Document document);

        /// <summary>
        /// Delete an object from the store.
        /// </summary>
        void Delete(object obj);

        /// <summary>
        /// Queries documents
        /// </summary>
        /// <returns></returns>
        IQueryable<Document> Load();

        /// <summary>
        /// Queries documents for a specific type
        /// </summary>
        IEnumerable<T> Load<T>(Func<IQueryable<Document>, IEnumerable<Document>> query = null) where T : class;

        /// <summary>
        /// Queries a single document for a specific type
        /// </summary>
        T Load<T>(Func<IQueryable<Document>, Document> query) where T : class;

        /// <summary>
        /// Queries a specific index.
        /// </summary>
        /// <typeparam name="TIndex">The index to query over.</typeparam>
        IQueryable<TIndex> QueryIndex<TIndex>() where TIndex : IIndex;

        IQuery Query();
        T As<T>(Document doc) where T : class;
        IEnumerable<T> As<T>(IEnumerable<Document> doc) where T : class;

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        void Commit();

        /// <summary>
        /// Commits the current transaction asynchromously
        /// </summary>
        Task CommitAsync();
    }
}
