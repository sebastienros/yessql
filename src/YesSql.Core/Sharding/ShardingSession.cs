using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Core.Data.Models;
using YesSql.Core.Indexes;
using YesSql.Core.Services;

namespace YesSql.Core.Sharding
{
    public class ShardingSession : ISession
    {
        private readonly IDictionary<string, ISession> _sessions;
        private readonly IShardStrategy _shardStrategy;
        private readonly IStore _store;

        public ShardingSession(IDictionary<string, ISession> sessions, IShardStrategy shardStrategy, IStore store)
        {
            _sessions = sessions;
            _shardStrategy = shardStrategy;
            _store = store;
        }

        public void Dispose()
        {
            foreach (var session in _sessions.Values)
            {
                try
                {
                    session.Dispose();
                }
                catch
                {
                    // todo: log exception
                }
            }
        }

        public void Save(Document document)
        {
            throw new NotImplementedException();
        }

        public void Save(object obj)
        {
            var shard = _shardStrategy.ShardSelectionStrategy.Select(obj);
            var session = _sessions[shard];
            session.Save(obj);
        }

        public void Delete(Document document)
        {
            throw new NotImplementedException();
        }

        public void Delete(object obj)
        {
            var shard = _shardStrategy.ShardSelectionStrategy.Select(obj);
            var session = _sessions[shard];
            session.Save(obj);
        }

        public IQueryable<Document> Load()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> Load<T>(Func<IQueryable<Document>, IEnumerable<Document>> query = null)
            where T : class
        {
            throw new NotImplementedException();
        }

        public T Load<T>(Func<IQueryable<Document>, Document> query) where T : class
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TResult> QueryByMappedIndex<TIndex, TResult>(
            Func<IQueryable<TIndex>, IQueryable<TIndex>> query)
            where TIndex : class, IHasDocumentIndex
            where TResult : class
        {
            return _sessions.Values.SelectMany(x => x.QueryByMappedIndex<TIndex, TResult>(query));
        }

        public TResult QueryByMappedIndex<TIndex, TResult>(Func<IQueryable<TIndex>, TIndex> query)
            where TIndex : class, IHasDocumentIndex
            where TResult : class
        {
            return _sessions.Values.Select(x => x.QueryByMappedIndex<TIndex, TResult>(query)).FirstOrDefault();
        }

        public IEnumerable<TResult> QueryByReducedIndex<TIndex, TResult>(
            Func<IQueryable<TIndex>, IQueryable<TIndex>> query)
            where TIndex : class, IHasDocumentsIndex
            where TResult : class
        {
            return _sessions.Values.SelectMany(x => x.QueryByReducedIndex<TIndex, TResult>(query));
        }

        public IEnumerable<TResult> QueryByReducedIndex<TIndex, TResult>(Func<IQueryable<TIndex>, TIndex> query)
            where TIndex : class, IHasDocumentsIndex
            where TResult : class
        {
            return _sessions.Values.SelectMany(x => x.QueryByReducedIndex<TIndex, TResult>(query));
        }

        public IQueryable<TIndex> QueryIndex<TIndex>() where TIndex : IIndex
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            foreach (var session in _sessions.Values)
            {
                session.Commit();
            }
        }

        public Task CommitAsync()
        {
            throw new NotImplementedException();
        }
    }
}
