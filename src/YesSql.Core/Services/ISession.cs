using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Core.Data.Models;
using YesSql.Core.Indexes;

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
        IQueryable<Document> QueryDocument();

        /// <summary>
        /// Queries documents for a specific type
        /// </summary>
        IEnumerable<T> QueryDocument<T>(Func<IQueryable<Document>, IEnumerable<Document>> query = null) where T : class;

        T QueryDocument<T>(Func<IQueryable<Document>, Document> query) where T : class;

        /// <summary>
        /// Queries documents for a specific type based on a mapped index
        /// </summary>
        IEnumerable<TResult> QueryByMappedIndex<TIndex, TResult>(Func<IQueryable<TIndex>, IQueryable<TIndex>> query)
            where TIndex : class, IHasDocumentIndex
            where TResult : class
            ;

        /// <summary>
        /// Queries documents for a specific type based on a mapped index
        /// </summary>
        TResult QueryByMappedIndex<TIndex, TResult>(Func<IQueryable<TIndex>, TIndex> query)
            where TIndex : class, IHasDocumentIndex
            where TResult : class
            ;

        /// <summary>
        /// Queries documents for a specific type based on a reduced index
        /// </summary>
        IEnumerable<TResult> QueryByReducedIndex<TIndex, TResult>(Func<IQueryable<TIndex>, IQueryable<TIndex>> query)
            where TIndex : class, IHasDocumentsIndex
            where TResult : class
            ;

        /// <summary>
        /// Queries documents for a specific type based on a reduced index
        /// </summary>
        IEnumerable<TResult> QueryByReducedIndex<TIndex, TResult>(Func<IQueryable<TIndex>, TIndex> query)
            where TIndex : class, IHasDocumentsIndex
            where TResult : class
            ;

        /// <summary>
        /// Queries a specific index.
        /// </summary>
        /// <typeparam name="TIndex">The index to query over.</typeparam>
        IQueryable<TIndex> QueryIndex<TIndex>() where TIndex : IIndex;

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
