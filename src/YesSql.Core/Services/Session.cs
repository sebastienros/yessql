using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using YesSql.Core.Commands;
using YesSql.Core.Data;
using YesSql.Core.Indexes;
using YesSql.Core.Query;
using YesSql.Core.Serialization;
using YesSql.Core.Sql;
using YesSql.Core.Storage;

namespace YesSql.Core.Services
{
    public class Session : ISession
    {
        private DbTransaction _transaction;

        private readonly IdentityMap _identityMap = new IdentityMap();
        private readonly List<IIndexCommand> _commands = new List<IIndexCommand>();
        private readonly IDictionary<IndexDescriptor, IList<MapState>> _maps;
        private readonly HashSet<object> _saved = new HashSet<object>();
        private readonly HashSet<object> _updated = new HashSet<object>();
        private readonly HashSet<object> _deleted = new HashSet<object>();
        private readonly IDocumentStorage _storage;
        private readonly Store _store;
        private readonly ISqlDialect _dialect;
        private readonly IsolationLevel _isolationLevel;
        private readonly DbConnection _connection;

        protected bool _cancel;

        public Session(Func<ISession, IDocumentStorage> storage, Store store, IsolationLevel isolationLevel)
        {
            _storage = storage(this);
            _store = store;
            _isolationLevel = isolationLevel;

            _maps = new Dictionary<IndexDescriptor, IList<MapState>>();

            _connection = _store.Configuration.ConnectionFactory.CreateConnection();
            _dialect = SqlDialectFactory.For(_connection);
        }

        public DbTransaction Transaction
        {
            get
            {
                Demand();
                return _transaction;
            }
        }

        public void Save(object entity)
        {
            // is it a new object?
            if (!_identityMap.HasEntity(entity))
            {
                // already beging saved?
                if (_saved.Contains(entity))
                {
                    return;
                }

                // then assign it an identifier
                var accessor = _store.GetIdAccessor(entity.GetType(), "Id");
                if (accessor != null)
                {
                    var id = accessor.Get(entity);

                    // do we need to track the entity
                    if (id > 0)
                    {
                        _identityMap.Add(id, entity);
                        _updated.Add(entity);
                        return;
                    }
                    else
                    {
                        // it's a new entity
                        id = _store.GetNextId();
                        accessor.Set(entity, id);
                    }
                }

                _saved.Add(entity);
            }
            else
            {
                // update it
                _updated.Add(entity);
            }
        }

        private async Task SaveEntityAsync(object entity, bool update)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("obj");
            }

            var index = entity as IIndex;
            var document = entity as Document;

            if (document != null)
            {
                throw new ArgumentException("A document should not be saved explicitely");
            }
            else if (index != null)
            {
                throw new ArgumentException("An index should not be saved explicitely");
            }
            else
            {
                // if the object is not new, reload to get the old map
                int id;
                if(_identityMap.TryGetDocumentId(entity, out id))
                {
                    if (update)
                    {
                        var oldDoc = await GetDocumentByIdAsync(id);
                        var oldObj = await _storage.GetAsync(id, entity.GetType());

                        // Update map index
                        MapDeleted(oldDoc, oldObj);
                        MapNew(oldDoc, entity);

                        // Save entity
                        await _storage.UpdateAsync(new IdentityDocument(id, entity));
                    }
                }
                else
                {
                    var doc = new Document
                    {
                        Type = entity.GetType().SimplifiedTypeName()
                    };

                    // Get the entity's Id if assigned
                    var accessor = _store.GetIdAccessor(entity.GetType(), "Id");
                    if(accessor != null)
                    {
                        doc.Id = accessor.Get(entity);
                    }
                    else
                    {
                        doc.Id = _store.GetNextId();
                    }

                    await new CreateDocumentCommand(doc, _store.Configuration.TablePrefix).ExecuteAsync(_connection, _transaction);
                    await _storage.CreateAsync(new IdentityDocument(doc.Id, entity));

                    // Track the newly created object
                    _identityMap.Add(doc.Id, entity);

                    MapNew(doc, entity);
                }
            }
        }

        private async Task<Document> GetDocumentByIdAsync(int id)
        {
            var command = $"select * from [{_store.Configuration.TablePrefix}Document] where Id = @Id";
            var result = await _connection.QueryAsync<Document>(command, new { Id = id }, _transaction);
            return result.FirstOrDefault();
        }

        public void Delete(object obj)
        {
            _deleted.Add(obj);
        }

        private async Task DeleteEntityAsync(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            else if (obj is IIndex)
            {
                throw new ArgumentException("Can't call DeleteEntity on an Index");
            }
            else
            {
                var accessor = _store.GetIdAccessor(obj.GetType(), "Id");
                if (accessor == null)
                {
                    throw new InvalidOperationException("Could not delete object as it doesn't have an Id property");
                }

                var id = accessor.Get(obj);
                var doc = await GetDocumentByIdAsync(id);

                if (doc != null)
                {
                    await _storage.DeleteAsync(new IdentityDocument(id, obj));

                    // Untrack the deleted object
                    _identityMap.Remove(id, obj);

                    // Update impacted indexes
                    MapDeleted(doc, obj);

                    // The command needs to come after any index deletiong because of the database constraints
                    _commands.Add(new DeleteDocumentCommand(doc, _store.Configuration.TablePrefix));
                }
            }
        }

        public async Task<IEnumerable<T>> GetAsync<T>(IEnumerable<int> ids) where T : class
        {
            // Auto-flush
            await CommitAsync(keepTracked: true);

            var result = new List<T>();

            if (ids == null || !ids.Any())
            {
                return result;
            }

            // Are all the objects already in cache?
            IEnumerable<object> cached = ids.Select(id =>
            {
                object entity;
                if (_identityMap.TryGetEntityById(id, out entity))
                {
                    return entity;
                }
                return null;
            });

            if (!cached.Any(x => x == null))
            {
                foreach (T item in cached)
                {
                    result.Add(item);
                }
            }

            // Some documents might not be in cache, load all of them from storage, then resolve local maps
            var items = (await _storage.GetAsync<T>(ids.ToArray())).ToArray();

            // if the document has an Id property, set it back
            var accessor = _store.GetIdAccessor(typeof(T), "Id");

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var id = ids.ElementAt(i);

                object entity;

                if (_identityMap.TryGetEntityById(id, out entity))
                {
                    result.Add((T)entity);
                }
                else
                {
                    if (accessor != null)
                    {
                        accessor.Set(item, id);
                    }

                    // track the loaded object
                    _identityMap.Add(id, item);

                    result.Add(item);
                }
            }

            return result;
        }

        public IQuery QueryAsync()
        {
            Demand();

            return new DefaultQuery(_connection, _transaction, this, _store.Configuration.TablePrefix);
        }

        public void Dispose()
        {
            try
            {
                if (!_cancel)
                {
                    // execute pending commands
                    CommitAsync().Wait();

                    if (_transaction != null)
                    {
                        _transaction.Commit();
                    }
                }
                else
                {
                    if (_transaction != null)
                    {
                        _transaction.Rollback();
                    }
                }
            }
            finally
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }

                if (_connection != null)
                {
                    if (_store.Configuration.ConnectionFactory.Disposable)
                    {
                        _connection.Dispose();
                    }
                    else
                    {
                        _connection.Close();
                    }
                }
            }
        }

        public async Task CommitAsync(bool keepTracked = false)
        {
            if(_saved.Count == 0 && _updated.Count == 0 && _deleted.Count == 0)
            {
                return;
            }

            // saving all updated entities
            foreach (var obj in _updated)
            {
                if (!_deleted.Contains(obj))
                {
                    await SaveEntityAsync(obj, update: true);
                }
            }

            // saving all pending entities
            foreach (var obj in _saved)
            {
                await SaveEntityAsync(obj, update: false);
            }

            // deleting all pending entities
            foreach (var obj in _deleted)
            {
                await DeleteEntityAsync(obj);
            }

            // compute all reduce indexes
            await ReduceAsync();

            foreach(var command in _commands.OrderBy(x => x.ExecutionOrder))
            {
                await command.ExecuteAsync(_connection, _transaction);
            }

            if(keepTracked)
            {
                foreach(var saved in _saved)
                {
                    _updated.Add(saved);
                }
            }
            else
            {
                _updated.Clear();
            }

            _saved.Clear();
            _deleted.Clear();
            _commands.Clear();
        }

        private async Task ReduceAsync()
        {
            // loop over each Indexer used by new objects
            foreach (var descriptor in _maps.Keys)
            {
                // if the descriptor has no reduce behavior, ignore it
                if (descriptor.Reduce == null)
                    continue;

                if (descriptor.GroupKey == null)
                {
                    throw new InvalidOperationException(
                        "A map/reduce index must declare at least one property with a GroupKey attribute: " +
                        descriptor.Type.FullName);
                }

                // a groupping method for the current descriptor
                var descriptorGroup = GetGroupingMetod(descriptor);

                // list all available grouping keys in the current set
                var allKeysForDescriptor =
                    _maps[descriptor].Select(x => x.Map).Select(descriptorGroup).Distinct().ToArray();

                // reduce each group, will result in one Reduced index per group
                foreach (var currentKey in allKeysForDescriptor)
                {
                    // group all mapped indexes
                    var newMapsGroup =
                        _maps[descriptor].Where(x => x.State == MapStates.New).Select(x => x.Map).Where(
                            x => descriptorGroup(x).Equals(currentKey)).ToArray();
                    var deletedMapsGroup =
                        _maps[descriptor].Where(x => x.State == MapStates.Delete).Select(x => x.Map).Where(
                            x => descriptorGroup(x).Equals(currentKey)).ToArray();
                    var updatedMapsGroup =
                        _maps[descriptor].Where(x => x.State == MapStates.Update).Select(x => x.Map).Where(
                            x => descriptorGroup(x).Equals(currentKey)).ToArray();

                    // todo: if an updated object got his Key changed, then apply a New to the new value group
                    // and a Delete to the old value group. Otherwise apply Update to the current value group

                    IIndex index = null;

                    if (newMapsGroup.Any())
                    {
                        // reducing an already groupped set (technically the reduction should contain the grouping step, but by design ...)
                        index = descriptor.Reduce(newMapsGroup.GroupBy(descriptorGroup).First());

                        if (index == null)
                        {
                            throw new InvalidOperationException(
                                "The reduction on a grouped set shoud have resulted in a unique result"
                                );
                        }
                    }

                    ReduceIndex dbIndex = await ReduceForAsync(descriptor, currentKey);

                    // if index present in db and new objects, reduce them
                    if (dbIndex != null && index != null)
                    {
                        // reduce over the two objects
                        var reductions = new[] { dbIndex, index };

                        var grouppedReductions = reductions.GroupBy(descriptorGroup).SingleOrDefault();

                        if (grouppedReductions == null)
                        {
                            throw new InvalidOperationException(
                                "The grouping on the db and in memory set shoud have resulted in a unique result");
                        }

                        index = descriptor.Reduce(grouppedReductions);

                        if (index == null)
                        {
                            throw new InvalidOperationException(
                                "The reduction on a grouped set shoud have resulted in a unique result");
                        }
                    }
                    else if (dbIndex != null)
                    {
                        index = dbIndex;
                    }

                    if (index != null)
                    {
                        // are there any deleted object for this descriptor/group ?
                        if (deletedMapsGroup.Any())
                        {
                            index = descriptor.Delete(index, deletedMapsGroup.GroupBy(descriptorGroup).First());
                            // At this point, index can be null if the reduction returned a null index from Delete handler
                        }

                        // are there any updated object for this descriptor/group ?
                        if (updatedMapsGroup.Any())
                        {
                            index = descriptor.Update(index, updatedMapsGroup.GroupBy(descriptorGroup).First());
                        }
                    }

                    var deletedDocumentIds = deletedMapsGroup.SelectMany(x => x.GetRemovedDocuments().Select(d => d.Id));
                    var addedDocumentIds = newMapsGroup.SelectMany(x => x.GetAddedDocuments().Select(d => d.Id));

                    if (dbIndex != null)
                    {
                        if (index == null)
                        {
                            _commands.Add(new DeleteReduceIndexCommand(dbIndex, _store.Configuration.TablePrefix));
                        }
                        else
                        {
                            index.Id = dbIndex.Id;

                            // Update both new and deleted linked documents
                            _commands.Add(new UpdateIndexCommand(index, addedDocumentIds, deletedDocumentIds, _store.Configuration.TablePrefix));
                        }
                    }
                    else
                    {
                        if (index != null)
                        {
                            // The index is new
                            _commands.Add(new CreateIndexCommand(index, addedDocumentIds, _store.Configuration.TablePrefix));
                        }
                    }
                }
            }
        }

        public async Task<ReduceIndex> ReduceForAsync(IndexDescriptor descriptor, object currentKey)
        {
            var name = _store.Configuration.TablePrefix + descriptor.IndexType.Name;
            var sql = $"select * from {name} where {descriptor.GroupKey.Name} = @currentKey";

            var index = await _connection.QueryAsync(descriptor.IndexType, sql, new { currentKey }, _transaction);
            return index.FirstOrDefault() as ReduceIndex;
        }

        /// <summary>
        /// Creates a Func{IIndex, object}; dynamically, based on GroupKey attributes
        /// this function will be used as the keySelector for Linq.Grouping
        /// </summary>
        private Func<IIndex, object> GetGroupingMetod(IndexDescriptor descriptor)
        {
            return _store.GroupMethods.GetOrAdd(descriptor.Type, (Type key) =>
            {
                // IIndex i => i
                var instance = Expression.Parameter(typeof(IIndex), "i");
                // i => ((TIndex)i)
                var convertInstance = Expression.Convert(instance, descriptor.GroupKey.DeclaringType);
                // i => ((TIndex)i).{Property}
                var property = Expression.Property(convertInstance, descriptor.GroupKey);
                // i => (object)(((TIndex)i).{Property})
                var convert = Expression.Convert(property, typeof(object));

                return Expression.Lambda<Func<IIndex, object>>(convert, instance).Compile();
            });
        }

        private void MapNew(Document document, object obj)
        {
            foreach (var descriptor in _store.Describe(obj.GetType()))
            {
                var mapped = descriptor.Map(obj);

                foreach (var index in mapped)
                {
                    if(index == null)
                    {
                        continue;
                    }

                    index.AddDocument(document);

                    // if the mapped elements are not meant to be reduced,
                    // then save them in db, as index
                    if (descriptor.Reduce == null)
                    {
                        if (index.Id == 0)
                        {
                            _commands.Add(new CreateIndexCommand(index, Enumerable.Empty<int>(), _store.Configuration.TablePrefix));
                        }
                        else
                        {
                            _commands.Add(new UpdateIndexCommand(index, Enumerable.Empty<int>(), Enumerable.Empty<int>(), _store.Configuration.TablePrefix));
                        }
                    }
                    else
                    {
                        // save for later reducing
                        IList<MapState> listmap;
                        if (!_maps.TryGetValue(descriptor, out listmap))
                        {
                            _maps.Add(descriptor, listmap = new List<MapState>());
                        }

                        listmap.Add(new MapState(index, MapStates.New));
                    }
                }
            }
        }

        /// <summary>
        /// Update map and reduce indexes when an entity is deleted.
        /// </summary>
        private void MapDeleted(Document document, object obj)
        {
            foreach (var descriptor in _store.Describe(obj.GetType()))
            {
                // If the mapped elements are not meant to be reduced, delete
                if (descriptor.Reduce == null || descriptor.Delete == null)
                {
                    _commands.Add(new DeleteMapIndexCommand(descriptor.IndexType, document.Id, _store.Configuration.TablePrefix));
                }
                else
                {
                    var mapped = descriptor.Map(obj);

                    foreach (var index in mapped)
                    {
                        // save for later reducing
                        IList<MapState> listmap;
                        if (!_maps.TryGetValue(descriptor, out listmap))
                        {
                            _maps.Add(descriptor, listmap = new List<MapState>());
                        }

                        listmap.Add(new MapState(index, MapStates.Delete));
                        index.RemoveDocument(document);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new transaction if none has been yet
        /// </summary>
        public DbTransaction Demand()
        {
            if (_transaction == null)
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }

                _transaction = _connection.BeginTransaction(_isolationLevel);
            }

            return _transaction;
        }

        public void Cancel()
        {
            _cancel = true;
        }

        public void ExecuteMigration(Action<SchemaBuilder> migration, bool throwException = true)
        {
            Demand();

            try
            {
                var schemaBuilder = new SchemaBuilder(_connection, _transaction, _store.Configuration.TablePrefix);
                migration(schemaBuilder);
            }
            catch
            {
                if (throwException)
                {
                    throw;
                }
            }
        }

    }

}
