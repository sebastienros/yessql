using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Commands;
using YesSql.Data;
using YesSql.Indexes;
using YesSql.Serialization;
using YesSql.Services;

namespace YesSql
{
    public class Session : ISession
    {
        private IDbTransaction _transaction;

        private readonly IdentityMap _identityMap = new IdentityMap();
        private readonly List<IIndexCommand> _commands = new List<IIndexCommand>();
        private readonly IDictionary<IndexDescriptor, IList<MapState>> _maps = new Dictionary<IndexDescriptor, IList<MapState>>();
        private readonly HashSet<object> _saved = new HashSet<object>();
        private readonly HashSet<object> _updated = new HashSet<object>();
        private readonly HashSet<object> _deleted = new HashSet<object>();
        protected readonly Dictionary<string, IEnumerable<IndexDescriptor>> _descriptors = new Dictionary<string, IEnumerable<IndexDescriptor>>();
        internal readonly Store _store;
        private volatile bool _disposed;
        private IsolationLevel _isolationLevel;
        private DbConnection _connection;
        private ISqlDialect _dialect;
        protected bool _cancel;
        protected List<IIndexProvider> _indexes;
        protected string _tablePrefix;

        public Session(Store store, IsolationLevel isolationLevel)
        {
            _store = store;
            _isolationLevel = isolationLevel;
            _tablePrefix = _store.Configuration.TablePrefix;
        }

        public ISession RegisterIndexes(params IIndexProvider[] indexProviders)
        {
            foreach (var indexProvider in indexProviders)
            {
                if (indexProvider.CollectionName == null)
                {
                    indexProvider.CollectionName = CollectionHelper.Current.GetSafeName();
                }
            }

            if (_indexes == null)
            {
                _indexes = new List<IIndexProvider>();
            }

            _indexes.AddRange(indexProviders);

            return this;
        }

        public void Save(object entity)
        {
            CheckDisposed();

            // already being saved or updated?
            if (_saved.Contains(entity) || _updated.Contains(entity))
            {
                return;
            }

            // is it a new object?
            if (_identityMap.TryGetDocumentId(entity, out var id))
            {
                _updated.Add(entity);
                return;
            }

            // Does it have a valid identifier?
            var accessor = _store.GetIdAccessor(entity.GetType(), "Id");
            if (accessor != null)
            {
                id = accessor.Get(entity);

                if (id > 0)
                {
                    _identityMap.Add(id, entity);
                    _updated.Add(entity);
                    return;
                }
            }

            // Then assign a new identifier if it has one
            if (accessor != null)
            {
                // it's a new entity
                var collection = CollectionHelper.Current.GetSafeName();
                id = _store.GetNextId(this, collection);
                accessor.Set(entity, id);
                _identityMap.Add(id, entity);
            }

            _saved.Add(entity);
        }

        public bool Import(object entity, int id = 0)
        {
            CheckDisposed();

            // already known?
            if (_saved.Contains(entity) || _updated.Contains(entity))
            {
                return false;
            }

            if (id != 0)
            {
                _identityMap.Add(id, entity);
                _updated.Add(entity);

                return true;
            }
            else
            {
                // Does it have a valid identifier?
                var accessor = _store.GetIdAccessor(entity.GetType(), "Id");
                if (accessor != null)
                {
                    id = accessor.Get(entity);

                    if (id > 0)
                    {
                        _identityMap.Add(id, entity);
                        _updated.Add(entity);
                        return true;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid 'Id' value: {id}");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Objects without an 'Id' property can't be imported if no 'id' argument is provided.");
                }
            }
        }

        private async Task SaveEntityAsync(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("obj");
            }

            if (entity is Document document)
            {
                throw new ArgumentException("A document should not be saved explicitely");
            }

            if (entity is IIndex index)
            {
                throw new ArgumentException("An index should not be saved explicitely");
            }

            var doc = new Document
            {
                Type = entity.GetType().SimplifiedTypeName()
            };

            // Get the entity's Id if assigned
            var accessor = _store.GetIdAccessor(entity.GetType(), "Id");
            if (accessor != null)
            {
                doc.Id = accessor.Get(entity);
            }
            else
            {
                var collection = CollectionHelper.Current.GetSafeName();
                doc.Id = _store.GetNextId(this, collection);
            }

            await DemandAsync();

            doc.Content = Store.Configuration.ContentSerializer.Serialize(entity);

            await new CreateDocumentCommand(doc, _tablePrefix).ExecuteAsync(_connection, _transaction, _dialect);

            await MapNew(doc, entity);
        }

        private async Task UpdateEntityAsync(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("obj");
            }

            var index = entity as IIndex;

            if (entity is Document document)
            {
                throw new ArgumentException("A document should not be saved explicitely");
            }

            if (index != null)
            {
                throw new ArgumentException("An index should not be saved explicitely");
            }

            // Reload to get the old map
            if (!_identityMap.TryGetDocumentId(entity, out var id))
            {
                throw new InvalidOperationException("The object to update was not found in identity map.");
            }

            var oldDoc = await GetDocumentByIdAsync(id);

            if (oldDoc == null)
            {
                throw new InvalidOperationException("Incorrect attempt to update an object that doesn't exist. Ensure a new object was not saved with an identifier value.");
            }

            var oldObj = Store.Configuration.ContentSerializer.Deserialize(oldDoc.Content, entity.GetType());

            // Update map index
            await MapDeleted(oldDoc, oldObj);

            await MapNew(oldDoc, entity);

            await DemandAsync();

            oldDoc.Content = Store.Configuration.ContentSerializer.Serialize(entity);
            await new UpdateDocumentCommand(oldDoc, Store.Configuration.TablePrefix).ExecuteAsync(_connection, _transaction, _dialect);
        }

        private async Task<Document> GetDocumentByIdAsync(int id)
        {
            await DemandAsync();

            var documentTable = CollectionHelper.Current.GetPrefixedName(YesSql.Store.DocumentTable);

            var command = "select * from " + _dialect.QuoteForTableName(_tablePrefix + documentTable) + " where " + _dialect.QuoteForColumnName("Id") + " = @Id";
            var key = new WorkerQueryKey(nameof(GetDocumentByIdAsync), new [] { id });
            var result = await _store.ProduceAsync(key, () => _connection.QueryAsync<Document>(command, new { Id = id }, _transaction));

            return result.FirstOrDefault();
        }

        public void Delete(object obj)
        {
            CheckDisposed();

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
                if (!_identityMap.TryGetDocumentId(obj, out var id))
                {
                    var accessor = _store.GetIdAccessor(obj.GetType(), "Id");
                    if (accessor == null)
                    {
                        throw new InvalidOperationException("Could not delete object as it doesn't have an Id property");
                    }

                    id = accessor.Get(obj);
                }

                var doc = await GetDocumentByIdAsync(id);

                if (doc != null)
                {
                    // Untrack the deleted object
                    _identityMap.Remove(id, obj);

                    // Update impacted indexes
                    await MapDeleted(doc, obj);

                    // The command needs to come after any index deletiong because of the database constraints
                    _commands.Add(new DeleteDocumentCommand(doc, _tablePrefix));
                }
            }
        }

        public async Task<IEnumerable<T>> GetAsync<T>(int[] ids) where T : class
        {
            if (ids == null || !ids.Any())
            {
                return Enumerable.Empty<T>();
            }

            CheckDisposed();

            // Auto-flush
            await CommitAsync();

            await DemandAsync();

            var documentTable = CollectionHelper.Current.GetPrefixedName(YesSql.Store.DocumentTable);
            var command = "select * from " + _dialect.QuoteForTableName(_tablePrefix + documentTable) + " where " + _dialect.QuoteForColumnName("Id") + " " + _dialect.InOperator("@Ids");

            var key = new WorkerQueryKey(nameof(GetAsync), ids);
            var documents = await _store.ProduceAsync(key, () =>
            {
                return _connection.QueryAsync<Document>(command, new { Ids = ids }, _transaction);
            });

            return Get<T>(documents.ToArray());
        }

        public IEnumerable<T> Get<T>(IList<Document> documents) where T : class
        {
            if (documents == null || !documents.Any())
            {
                return Enumerable.Empty<T>();
            }

            var result = new List<T>();

            var accessor = _store.GetIdAccessor(typeof(T), "Id");
            var typeName = typeof(T).SimplifiedTypeName();

            // Are all the objects already in cache?
            foreach (var d in documents)
            {
                if (typeof(T) != typeof(object) && !String.Equals(typeName, d.Type, StringComparison.Ordinal))
                {
                    continue;
                }

                if (_identityMap.TryGetEntityById(d.Id, out var entity))
                {
                    result.Add((T)entity);
                }
                else
                {
                    T item;

                    // If no type is specified, use the one from the document
                    if (typeof(T) == typeof(object))
                    {
                        var itemType = Type.GetType(d.Type) ?? typeof(object);
                        accessor = _store.GetIdAccessor(itemType, "Id");

                        item = (T)Store.Configuration.ContentSerializer.Deserialize(d.Content, itemType);
                    }
                    else
                    {
                        item = (T)Store.Configuration.ContentSerializer.Deserialize(d.Content, typeof(T));
                    }

                    if (accessor != null)
                    {
                        accessor.Set(item, d.Id);
                    }

                    // track the loaded object
                    _identityMap.Add(d.Id, item);

                    result.Add(item);
                }
            };
            
            return result;
        }

        public IQuery Query()
        {
            return new DefaultQuery(_connection, _transaction, this, _tablePrefix);
        }

        public IQuery<T> ExecuteQuery<T>(ICompiledQuery<T> compiledQuery) where T : class
        {
            if (compiledQuery == null)
            {
                throw new ArgumentNullException(nameof(compiledQuery));
            }

            var queryState = _store.CompiledQueries.GetOrAdd(compiledQuery.GetType(), t =>
            {
                var localQuery = ((IQuery)new DefaultQuery(_connection, _transaction, this, _tablePrefix)).For<T>(false);
                var defaultQuery = (DefaultQuery.Query<T>)compiledQuery.Query().Compile().Invoke(localQuery);
                
                return defaultQuery._query._queryState;
            });

            queryState = queryState.Clone();

            IQuery newQuery = new DefaultQuery(_connection, _transaction, this, _tablePrefix, queryState, compiledQuery);
            return newQuery.For<T>(false);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Session));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                if (!_cancel)
                {
                    if (HasWork())
                    {
                        // Execute pending commands. This is a sync call over async
                        // which is not recommended. Prefer to call CommitAsync() before 
                        // disposing the session.

                        CommitAsync().Wait();
                    }

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
                _disposed = true;

                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }

                if (_connection != null)
                {
                    _store.Configuration.ConnectionFactory.CloseConnection(_connection);
                    _connection = null;
                }

                Release();
            }            
        }

        /// <summary>
        /// Called when the instance is available to an object pool.
        /// </summary>
        internal void Release()
        {
            _updated.Clear();
            _saved.Clear();
            _deleted.Clear();
            _commands.Clear();
            _maps.Clear();

            _identityMap.Clear();
            _descriptors.Clear();
            _indexes?.Clear();
            _store.ReleaseSession(this);
        }

        /// <summary>
        /// Called when the instance is reused from an object pool and doesn't go
        /// through the constructor.
        /// </summary>
        internal void StartLease(IsolationLevel isolationLevel)
        {
            _disposed = false;
            _cancel = false;
            _isolationLevel = isolationLevel;
        }

        public async Task CommitAsync()
        {
            CheckDisposed();

            if (!HasWork())
            {
                return;
            }

            // saving all updated entities
            foreach (var obj in _updated)
            {
                if (!_deleted.Contains(obj))
                {
                    await UpdateEntityAsync(obj);
                }
            }

            // saving all pending entities
            foreach (var obj in _saved)
            {
                await SaveEntityAsync(obj);
            }

            // deleting all pending entities
            foreach (var obj in _deleted)
            {
                await DeleteEntityAsync(obj);
            }

            // compute all reduce indexes
            await ReduceAsync();

            await DemandAsync();
            
            foreach (var command in _commands.OrderBy(x => x.ExecutionOrder))
            {
                await command.ExecuteAsync(_connection, _transaction, _dialect);
            }

            _updated.Clear();
            _saved.Clear();
            _deleted.Clear();
            _commands.Clear();
            _maps.Clear();
        }

        /// <summary>
        /// Whether the current session has data to flush or not.
        /// </summary>
        internal bool HasWork()
        {
            return
                _saved.Count != 0 ||
                _updated.Count != 0 ||
                _deleted.Count != 0
                ;
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
                                "The reduction on a grouped set should have resulted in a unique result"
                                );
                        }
                    }

                    var dbIndex = await ReduceForAsync(descriptor, currentKey);

                    // if index present in db and new objects, reduce them
                    if (dbIndex != null && index != null)
                    {
                        // reduce over the two objects
                        var reductions = new[] { dbIndex, index };

                        var grouppedReductions = reductions.GroupBy(descriptorGroup).SingleOrDefault();

                        if (grouppedReductions == null)
                        {
                            throw new InvalidOperationException(
                                "The grouping on the db and in memory set should have resulted in a unique result");
                        }

                        index = descriptor.Reduce(grouppedReductions);

                        if (index == null)
                        {
                            throw new InvalidOperationException(
                                "The reduction on a grouped set should have resulted in a unique result");
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

                    var deletedDocumentIds = deletedMapsGroup.SelectMany(x => x.GetRemovedDocuments().Select(d => d.Id)).ToArray();
                    var addedDocumentIds = newMapsGroup.SelectMany(x => x.GetAddedDocuments().Select(d => d.Id)).ToArray();

                    if (dbIndex != null)
                    {
                        if (index == null)
                        {
                            _commands.Add(new DeleteReduceIndexCommand(dbIndex, _tablePrefix));
                        }
                        else
                        {
                            index.Id = dbIndex.Id;

                            var common = addedDocumentIds.Intersect(deletedDocumentIds).ToArray();
                            addedDocumentIds = addedDocumentIds.Where(x => !common.Contains(x)).ToArray();
                            deletedDocumentIds = deletedDocumentIds.Where(x => !common.Contains(x)).ToArray();

                            // Update updated, new and deleted linked documents
                            _commands.Add(new UpdateIndexCommand(index, addedDocumentIds, deletedDocumentIds, _tablePrefix));
                        }
                    }
                    else
                    {
                        if (index != null)
                        {
                            // The index is new
                            _commands.Add(new CreateIndexCommand(index, addedDocumentIds, _tablePrefix));
                        }
                    }
                }
            }
        }

        private async Task<ReduceIndex> ReduceForAsync(IndexDescriptor descriptor, object currentKey)
        {
            await DemandAsync();

            var name = _tablePrefix + descriptor.IndexType.Name;
            var sql = "select * from " + _dialect.QuoteForTableName(name) + " where " + _dialect.QuoteForColumnName(descriptor.GroupKey.Name) + " = @currentKey";

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

        /// <summary>
        /// Resolves all the descriptors registered on the Store and the Session
        /// </summary>
        private IEnumerable<IndexDescriptor> GetDescriptors(Type t)
        {
            var cacheKey = t.FullName + ":" + CollectionHelper.Current.GetSafeName();

            if (!_descriptors.TryGetValue(cacheKey, out var typedDescriptors))
            {
                typedDescriptors = _store.Describe(t);

                if (_indexes != null)
                {
                    var collection = CollectionHelper.Current.GetSafeName();
                    typedDescriptors = typedDescriptors.Union(_store.CreateDescriptors(t, collection, _indexes)).ToArray();
                }

                _descriptors.Add(cacheKey, typedDescriptors);
            }

            return typedDescriptors;
        }

        private async Task MapNew(Document document, object obj)
        {
            var descriptors = GetDescriptors(obj.GetType());

            foreach (var descriptor in descriptors)
            {
                var mapped = await descriptor.Map(obj);

                if (mapped != null)
                {
                    foreach (var index in mapped)
                    {
                        if (index == null)
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
                                _commands.Add(new CreateIndexCommand(index, Enumerable.Empty<int>(), _tablePrefix));
                            }
                            else
                            {
                                _commands.Add(new UpdateIndexCommand(index, Enumerable.Empty<int>(), Enumerable.Empty<int>(), _tablePrefix));
                            }
                        }
                        else
                        {
                            // save for later reducing
                            if (!_maps.TryGetValue(descriptor, out var listmap))
                            {
                                _maps.Add(descriptor, listmap = new List<MapState>());
                            }

                            listmap.Add(new MapState(index, MapStates.New));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update map and reduce indexes when an entity is deleted.
        /// </summary>
        private async Task MapDeleted(Document document, object obj)
        {
            var descriptors = GetDescriptors(obj.GetType());

            foreach (var descriptor in descriptors)
            {
                // If the mapped elements are not meant to be reduced, delete
                if (descriptor.Reduce == null || descriptor.Delete == null)
                {
                    _commands.Add(new DeleteMapIndexCommand(descriptor.IndexType, document.Id, _tablePrefix, _dialect));
                }
                else
                {
                    var mapped = await descriptor.Map(obj);

                    if (mapped != null)
                    {
                        foreach (var index in mapped)
                        {
                            // save for later reducing
                            if (!_maps.TryGetValue(descriptor, out var listmap))
                            {
                                _maps.Add(descriptor, listmap = new List<MapState>());
                            }

                            listmap.Add(new MapState(index, MapStates.Delete));
                            index.RemoveDocument(document);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new transaction if none has been yet
        /// </summary>
        public async Task<IDbTransaction> DemandAsync()
        {
            CheckDisposed();

            if (_transaction == null)
            {
                if (_connection == null)
                {
                    _connection = _store.Configuration.ConnectionFactory.CreateConnection() as DbConnection;

                    if (_connection == null)
                    {
                        throw new InvalidOperationException("The connection couldn't be covnerted to DbConnection");
                    }

                    // The dialect could already be initialized if the session is reused
                    if (_dialect == null)
                    {
                        _dialect = Store.Dialect;
                    }
                }

                if (_connection.State == ConnectionState.Closed)
                {
                    await _connection.OpenAsync();
                }

                // In the case of shared connections (InMemory) this can throw as the transation
                // might already be set by a concurrent thread on the same shared connection.
                _transaction = _connection.BeginTransaction(_isolationLevel);
            }

            return _transaction;
        }

        public void Cancel()
        {
            CheckDisposed();

            _cancel = true;
        }

        public IStore Store => _store;

        #region Storage implementation
        
        private struct IdString
        {
#pragma warning disable 0649
            public int Id;
            public string Content;
#pragma warning restore 0649
        }
        #endregion
    }
}
