using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Core.Data.Models;
using YesSql.Core.Indexes;
using YesSql.Core.Query;
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

        public IQueryable<TIndex> QueryIndex<TIndex>() where TIndex : IIndex
        {
            throw new NotImplementedException();
        }

        public IQuery Query()
        {
            throw new NotImplementedException();
        }

        public T As<T>(Document doc) where T : class
        {
            return _sessions.Values.First().As<T>(doc);
        }

        public IEnumerable<T> As<T>(IEnumerable<Document> doc) where T : class
        {
            return _sessions.Values.First().As<T>(doc);
        }

        public Task CommitAsync()
        {
            throw new NotImplementedException();
        }

        public Document Get(int id)
        {
            return _sessions.Select(x => x.Value.Get(id)).FirstOrDefault();
        }

        public T Get<T>(int id) where T : class
        {
            return _sessions.Select(x => x.Value.Get<T>(id)).FirstOrDefault();
        }

        public void Flush()
        {
            foreach (var session in _sessions.Values)
            {
                session.Flush();
            }
        }


        public void Cancel()
        {
            foreach (var session in _sessions.Values)
            {
                session.Cancel();
            }
        }
    }
}
