using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using YesSql.Commands;
using YesSql.Data;
using YesSql.Indexes;
using YesSql.Services;

namespace YesSql
{
    public class Session : ISession
    {
        private DbConnection _connection;
        private DbTransaction _transaction;

        internal List<IIndexCommand> _commands;
        private readonly Dictionary<string, SessionState> _collectionStates;
        private readonly SessionState _defaultState;
        private Dictionary<string, IEnumerable<IndexDescriptor>> _descriptors;
        internal readonly Store _store;
        private volatile bool _disposed;
        private bool _flushing;
        protected bool _cancel;
        protected bool _save;
        protected List<IIndexProvider> _indexes;

        protected string _tablePrefix;
        private readonly ISqlDialect _dialect;
        private readonly ILogger _logger;
        public IEnumerable<IndexDescriptor> ExtraIndexDescriptors { get; set; } = [];
        public IDocumentCommandHandler DocumentCommandHandler { get; set; }
        private readonly bool _withTracking;

        public Func<Type, string, Task<IEnumerable<IndexDescriptor>>> BuildExtraIndexDescriptors { get; set; }
        private readonly bool _enableThreadSafetyChecks;
        private int _asyncOperations = 0;
        private string _previousStackTrace = null;

        public Session(Store store, bool withTracking = true)
        {
            _store = store;
            _tablePrefix = _store.Configuration.TablePrefix;
            _dialect = store.Dialect;
            _logger = store.Configuration.Logger;
            _withTracking = withTracking;
            _defaultState = new SessionState();
            _enableThreadSafetyChecks = _store.Configuration.EnableThreadSafetyChecks;
            _collectionStates = new Dictionary<string, SessionState>()
            {
                [string.Empty] = _defaultState
            };
            DocumentCommandHandler = new DefaultDocumentCommandHandler();
        }

        public ISession RegisterIndexes(IIndexProvider[] indexProviders, string collection = null)
        {
            foreach (var indexProvider in indexProviders)
            {
                if (indexProvider.CollectionName == null)
                {
                    indexProvider.CollectionName = collection ?? string.Empty;
                }
            }

            _indexes ??= [];

            _indexes.AddRange(indexProviders);

            return this;
        }

        private SessionState GetState(string collection)
        {
            if (string.IsNullOrEmpty(collection))
            {
                return _defaultState;
            }

            if (!_collectionStates.TryGetValue(collection, out var state))
            {
                state = new SessionState();
                _collectionStates[collection] = state;
            }

            return state;
        }

        public void Save(object entity, bool checkConcurrency = false, string collection = null)
            => SaveAsync(entity, checkConcurrency, collection).GetAwaiter().GetResult();

        public async Task SaveAsync(object entity, bool checkConcurrency = false, string collection = null)
        {
            var state = GetState(collection);

            CheckDisposed();

            // already being saved or updated or tracked?
            if (state.Saved.Contains(entity) || state.Updated.Contains(entity))
            {
                return;
            }

            // remove from tracked entities if explicitly saved
            state.Tracked.Remove(entity);

            // is it a new object?
            if (state.IdentityMap.TryGetDocumentId(entity, out var id))
            {
                state.Updated.Add(entity);

                // If this entity needs to be checked for concurrency, track its version
                if (checkConcurrency || _store.Configuration.ConcurrentTypes.Contains(entity.GetType()))
                {
                    state.Concurrent.Add(id);
                }

                return;
            }

            // Does it have a valid identifier?
            var accessor = _store.GetIdAccessor(entity.GetType());
            if (accessor != null)
            {
                id = accessor.Get(entity);

                if (id > 0)
                {
                    // If we got an object from a different identity map, without change tracking, the object reference could be different than
                    // the one in the identity map, and the previous state.IdentityMap.TryGetDocumentId would have returned false.
                    // In this case we need to assume it's an updated object and not try to "Add" it to the identity map.

                    if (state.IdentityMap.TryGetEntityById(id, out var _))
                    {
                        throw new InvalidOperationException("An object with the same identity is already part of this transaction. Reload it before doing any changes on it.");
                    }

                    state.IdentityMap.AddEntity(id, entity);
                    state.Updated.Add(entity);

                    // If this entity needs to be checked for concurrency, track its version
                    if (checkConcurrency || _store.Configuration.ConcurrentTypes.Contains(entity.GetType()))
                    {
                        state.Concurrent.Add(id);
                    }

                    return;
                }
            }

            // It's a new entity
            id = await _store.GetNextIdAsync(collection);
            state.IdentityMap.AddEntity(id, entity);

            // Then assign a new identifier if it has one
            accessor?.Set(entity, id);

            state.Saved.Add(entity);
        }

        public bool Import(object entity, long id = 0, long version = 0, string collection = null)
        {
            CheckDisposed();

            var state = GetState(collection);

            // already known?
            if (state.IdentityMap.HasEntity(entity))
            {
                return false;
            }

            var doc = new Document
            {
                Type = Store.TypeService[entity.GetType()],
                Content = Store.Configuration.ContentSerializer.Serialize(entity)
            };

            // Import version
            if (version != 0)
            {
                doc.Version = version;
            }
            else
            {
                var versionAccessor = _store.GetVersionAccessor(entity.GetType());
                if (versionAccessor != null)
                {
                    doc.Version = versionAccessor.Get(entity);
                }
            }

            if (id != 0)
            {
                state.IdentityMap.AddEntity(id, entity);
                state.Updated.Add(entity);

                doc.Id = id;
                state.IdentityMap.AddDocument(doc);

                return true;
            }
            else
            {
                // Does it have a valid identifier?
                var accessor = _store.GetIdAccessor(entity.GetType());
                if (accessor != null)
                {
                    id = accessor.Get(entity);

                    if (id > 0)
                    {
                        state.IdentityMap.AddEntity(id, entity);
                        state.Updated.Add(entity);

                        doc.Id = id;
                        state.IdentityMap.AddDocument(doc);

                        return true;
                    }

                    throw new InvalidOperationException($"Invalid 'Id' value: {id}");
                }

                throw new InvalidOperationException("Objects without an 'Id' property can't be imported if no 'id' argument is provided.");
            }
        }

        public void Detach(object entity, string collection)
        {
            CheckDisposed();

            var state = GetState(collection);

            DetachInternal(entity, state);
        }

        public void Detach(IEnumerable<object> entries, string collection)
        {
            CheckDisposed();

            var state = GetState(collection);

            foreach (var entry in entries)
            {
                DetachInternal(entry, state);
            }
        }

        public void DetachAll(string collection)
        {
            CheckDisposed();

            var state = GetState(collection);

            state._concurrent?.Clear();
            state._saved?.Clear();
            state._updated?.Clear();
            state._tracked?.Clear();
            state._deleted?.Clear();
            state._identityMap?.Clear();
        }

        public async Task ResetAsync()
        {
            CheckDisposed();

            await ReleaseTransactionAsync();
            await ReleaseConnectionAsync();
        }

        private static void DetachInternal(object entity, SessionState state)
        {
            state.Saved.Remove(entity);
            state.Updated.Remove(entity);
            state.Tracked.Remove(entity);
            state.Deleted.Remove(entity);

            if (state.IdentityMap.TryGetDocumentId(entity, out var id))
            {
                state.IdentityMap.Remove(id, entity);
            }
        }

        private async Task SaveEntityAsync(object entity, string collection)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (entity is Document)
            {
                throw new ArgumentException("A document should not be saved explicitly");
            }

            if (entity is IIndex)
            {
                throw new ArgumentException("An index should not be saved explicitly");
            }

            var state = GetState(collection);

            var doc = new Document
            {
                Type = Store.TypeService[entity.GetType()]
            };

            if (!state.IdentityMap.TryGetDocumentId(entity, out var id))
            {
                throw new InvalidOperationException("The object to save was not found in identity map.");
            }

            doc.Id = id;

            await CreateConnectionAsync();

            var versionAccessor = _store.GetVersionAccessor(entity.GetType());
            if (versionAccessor != null)
            {
                doc.Version = versionAccessor.Get(entity);
            }

            if (doc.Version == 0)
            {
                doc.Version = 1;
            }

            versionAccessor?.Set(entity, doc.Version);

            doc.Content = Store.Configuration.ContentSerializer.Serialize(entity);

            _commands ??= [];

            _commands.Add(new CreateDocumentCommand(entity, doc, Store, collection, this));

            state.IdentityMap.AddDocument(doc);

            await MapNew(doc, entity, collection);
        }

        private async Task UpdateEntityAsync(object entity, bool tracked, string collection)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (entity is Document)
            {
                throw new ArgumentException("A document should not be saved explicitly");
            }

            if (entity is IIndex)
            {
                throw new ArgumentException("An index should not be saved explicitly");
            }

            var state = GetState(collection);

            // Reload to get the old map
            if (!state.IdentityMap.TryGetDocumentId(entity, out var id))
            {
                throw new InvalidOperationException("The object to update was not found in identity map.");
            }

            if (!state.IdentityMap.TryGetDocument(id, out var oldDoc))
            {
                oldDoc = await GetDocumentByIdAsync(id, collection);

                if (oldDoc == null)
                {
                    throw new InvalidOperationException("Incorrect attempt to update an object that doesn't exist. Ensure a new object was not saved with an identifier value.");
                }
            }

            var newContent = Store.Configuration.ContentSerializer.Serialize(entity);

            // if the document has already been updated or saved with this session (auto or intentional flush), ensure it has 
            // been changed before doing another query
            if (tracked && string.Equals(newContent, oldDoc.Content, StringComparison.Ordinal))
            {
                return;
            }

            long version = -1;

            if (state.Concurrent.Contains(id))
            {
                version = oldDoc.Version;

                var versionAccessor = _store.GetVersionAccessor(entity.GetType());
                if (versionAccessor != null)
                {
                    var localVersion = versionAccessor.Get(entity);

                    // if the version has been set, use it
                    if (localVersion != 0)
                    {
                        version = localVersion;
                    }
                }

                oldDoc.Version += 1;

                // apply the new version to the object
                if (versionAccessor != null)
                {
                    versionAccessor.Set(entity, oldDoc.Version);

                    newContent = Store.Configuration.ContentSerializer.Serialize(entity);
                }
            }

            var oldObj = Store.Configuration.ContentSerializer.Deserialize(oldDoc.Content, entity.GetType());

            // Update map index
            await MapDeleted(oldDoc, oldObj, collection);

            await MapNew(oldDoc, entity, collection);

            await CreateConnectionAsync();

            oldDoc.Content = newContent;

            _commands ??= [];

            _commands.Add(new UpdateDocumentCommand(entity, oldDoc, Store, version, collection, this));
        }

        private async Task<Document> GetDocumentByIdAsync(long id, string collection)
        {
            await CreateConnectionAsync();

            var documentTable = Store.Configuration.TableNameConvention.GetDocumentTable(collection);

            var command = "select * from " + _dialect.QuoteForTableName(_tablePrefix + documentTable, Store.Configuration.Schema) + " where " + _dialect.QuoteForColumnName("Id") + " = @Id";
            var key = new WorkerQueryKey(nameof(GetDocumentByIdAsync), id);

            try
            {
                var result = await _store.ProduceAsync(key, (key, state) =>
                {
                    var logger = state.Store.Configuration.Logger;

                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        logger.LogTrace(state.Command);
                    }

                    return state.Connection.QueryAsync<Document>(state.Command, state.Parameters, state.Transaction);
                },
                new { Store = _store, Connection = _connection, Transaction = _transaction, Command = command, Parameters = new { Id = id } });

                // Clone documents returned from ProduceAsync as they might be shared across sessions
                return result.FirstOrDefault()?.Clone();
            }
            catch
            {
                await CancelAsync();

                throw;
            }
        }

        public void Delete(object obj, string collection = null)
        {
            CheckDisposed();

            var state = GetState(collection);

            state.Deleted.Add(obj);
        }

        private async Task DeleteEntityAsync(object obj, string collection)
        {
            ArgumentNullException.ThrowIfNull(obj);

            if (obj is IIndex)
            {
                throw new ArgumentException("Can't call DeleteEntity on an Index");
            }

            var state = GetState(collection);

            if (!state.IdentityMap.TryGetDocumentId(obj, out var id))
            {
                var accessor = _store.GetIdAccessor(obj.GetType())
                    ?? throw new InvalidOperationException("Could not delete object as it doesn't have an Id property");

                id = accessor.Get(obj);
            }

            var doc = await GetDocumentByIdAsync(id, collection);

            if (doc != null)
            {
                // Untrack the deleted object
                state.IdentityMap.Remove(id, obj);

                // Update impacted indexes
                await MapDeleted(doc, obj, collection);

                _commands ??= [];

                // The command needs to come after any index deletion because of the database constraints
                _commands.Add(new DeleteDocumentCommand(obj, doc, Store, collection, this));
            }
        }

        public async Task<IEnumerable<T>> GetAsync<T>(long[] ids, string collection = null) where T : class
        {
            if (ids?.Length == 0)
            {
                return Enumerable.Empty<T>();
            }

            CheckDisposed();

            // Auto-flush
            await FlushAsync();

            await CreateConnectionAsync();

            var documentTable = Store.Configuration.TableNameConvention.GetDocumentTable(collection);

            var command = "select * from " + _dialect.QuoteForTableName(_tablePrefix + documentTable, _store.Configuration.Schema) + " where " + _dialect.QuoteForColumnName("Id") + " " + _dialect.InOperator("@Ids");

            var key = new WorkerQueryKey(nameof(GetAsync), ids);
            try
            {
                var documents = await _store.ProduceAsync(key, static (key, state) =>
                {
                    var logger = state.Store.Configuration.Logger;

                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        logger.LogTrace(state.Command);
                    }

                    return state.Connection.QueryAsync<Document>(state.Command, state.Parameters, state.Transaction);
                },
                new { Store = _store, Connection = _connection, Transaction = _transaction, Command = command, Parameters = new { Ids = ids } });

                // Clone documents returned from ProduceAsync as they might be shared across sessions
                var sortedDocuments = documents.Select(x => x.Clone())
                    .OrderBy(d => Array.IndexOf(ids, d.Id))
                    .ToList();

                return Get<T>(sortedDocuments, collection);
            }
            catch
            {
                await CancelAsync();

                throw;
            }
        }

        public IEnumerable<T> Get<T>(IList<Document> documents, string collection) where T : class
        {
            if (documents?.Count == 0)
            {
                return Enumerable.Empty<T>();
            }

            var result = new List<T>(documents.Count);
            var defaultAccessor = _store.GetIdAccessor(typeof(T));
            var typeName = Store.TypeService[typeof(T)];

            var state = GetState(collection);

            // Are all the objects already in cache?
            foreach (var d in documents)
            {
                if (_withTracking && state.IdentityMap.TryGetEntityById(d.Id, out var entity))
                {
                    result.Add((T)entity);
                }
                else
                {
                    T item;

                    IAccessor<long> accessor;
                    // If the document type doesn't match the requested one, check it's a base type
                    if (!string.Equals(typeName, d.Type, StringComparison.Ordinal))
                    {
                        var itemType = Store.TypeService[d.Type];

                        // Ignore the document if it can't be casted to the requested type
                        if (!typeof(T).IsAssignableFrom(itemType))
                        {
                            continue;
                        }

                        accessor = _store.GetIdAccessor(itemType);

                        item = (T)Store.Configuration.ContentSerializer.Deserialize(d.Content, itemType);
                    }
                    else
                    {
                        item = (T)Store.Configuration.ContentSerializer.Deserialize(d.Content, typeof(T));

                        accessor = defaultAccessor;
                    }

                    accessor?.Set(item, d.Id);

                    if (_withTracking)
                    {
                        // track the loaded object.
                        state.IdentityMap.AddEntity(d.Id, item);
                        state.IdentityMap.AddDocument(d);
                    }

                    result.Add(item);
                }
            }

            return result;
        }

        public IQuery Query(string collection = null)
        {
            return new DefaultQuery(this, _tablePrefix, collection);
        }

        public IQuery<T> ExecuteQuery<T>(ICompiledQuery<T> compiledQuery, string collection = null) where T : class
        {
            ArgumentNullException.ThrowIfNull(compiledQuery);

            var compiledQueryType = compiledQuery.GetType();

            var discriminator = NullableThumbprintFactory.GetNullableThumbprint(compiledQuery);

            var queryState = _store.CompiledQueries.GetOrAdd(discriminator, discriminator =>
            {
                var localQuery = ((IQuery)new DefaultQuery(this, _tablePrefix, collection)).For<T>(false);
                var defaultQuery = (DefaultQuery.Query<T>)compiledQuery.Query().Compile().Invoke(localQuery);
                return defaultQuery._query._queryState;
            })
            .Clone();

            IQuery newQuery = new DefaultQuery(this, queryState, compiledQuery);
            return newQuery.For<T>(false);
        }

        private void CheckDisposed()
        {
#pragma warning disable CA1513 // Use ObjectDisposedException throw helper
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Session));
            }
#pragma warning restore CA1513 // Use ObjectDisposedException throw helper
        }

        ~Session()
        {
            // Ensure the session gets disposed if the user cannot wrap the session in a using block.
            // For instance in OrchardCore the session is disposed from a middleware, so if an exception
            // is thrown in a middleware, it might not get triggered.
            Dispose(false);
        }

        public void Dispose(bool _)
        {
            // Do nothing if Dispose() was already called
            if (!_disposed)
            {
                try
                {
                    CommitOrRollbackTransactionAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    _connection = null;
                    _transaction = null;
                }
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public Task FlushAsync()
        {
            return FlushInternalAsync(false);
        }

        private async Task FlushInternalAsync(bool saving)
        {
            if (!HasWork())
            {
                return;
            }

            // prevent recursive calls in FlushAsync,
            // when auto-flush is triggered from an IndexProvider
            // for instance.

            if (_flushing)
            {
                return;
            }

            _flushing = true;

            // we only check if the session is disposed if 
            // there are no commands to commit.

            CheckDisposed();

            // Only check thread-safety if not called from SaveChangesAsync
            if (!saving)
            {
                EnterAsyncExecution();
            }

            try
            {
                // saving all tracked entities
                foreach (var collectionState in _collectionStates)
                {
                    var state = collectionState.Value;
                    var collection = collectionState.Key;

                    foreach (var obj in state.Tracked)
                    {
                        if (!state.Deleted.Contains(obj))
                        {
                            await UpdateEntityAsync(obj, true, collection);
                        }
                    }

                    // saving all updated entities
                    foreach (var obj in state.Updated)
                    {
                        if (!state.Deleted.Contains(obj))
                        {
                            await UpdateEntityAsync(obj, false, collection);
                        }
                    }

                    // saving all pending entities
                    foreach (var obj in state.Saved)
                    {
                        await SaveEntityAsync(obj, collection);
                    }

                    // deleting all pending entities
                    foreach (var obj in state.Deleted)
                    {
                        await DeleteEntityAsync(obj, collection);
                    }
                }

                // compute all reduce indexes
                await ReduceAsync();

                await BeginTransactionAsync();

                BatchCommands();

                if (_commands != null)
                {
                    foreach (var command in _commands)
                    {
                        await command.ExecuteAsync(_connection, _transaction, _dialect, _logger);
                    }
                }
            }
            catch
            {
                await CancelAsync();

                throw;
            }
            finally
            {
                foreach (var state in _collectionStates.Values)
                {
                    // Track all saved and updated entities in case they are modified before
                    // CommitAsync is called
                    foreach (var saved in state.Saved)
                    {
                        state.Tracked.Add(saved);
                    }

                    foreach (var updated in state.Updated)
                    {
                        state.Tracked.Add(updated);
                    }

                    state.Saved.Clear();
                    state.Updated.Clear();
                    state.Deleted.Clear();
                    state.Maps.Clear();
                }

                _commands?.Clear();
                _flushing = false;

                // Only check thread-safety if not called from SaveChangesAsync
                if (!saving)
                {
                    ExitAsyncExecution();
                }
            }
        }

        private void BatchCommands()
        {
            if (_commands?.Count == 0)
            {
                return;
            }

            if (!_dialect.SupportsBatching || _store.Configuration.CommandsPageSize == 0)
            {
                return;
            }

            var batches = new List<IIndexCommand>();

            // holds the queries, parameters and actions returned by an IIndexCommand, until we know we can
            // add it to a batch if it fits the limits (page size and parameters boundaries)
            var localDbCommand = _connection.CreateCommand();
            var localQueries = new List<string>();
            var localActions = new List<Action<DbDataReader>>();

            var batch = new BatchCommand(_connection.CreateCommand());
            var index = 0;

            foreach (var command in _commands.OrderBy(x => x.ExecutionOrder))
            {
                index++;

                // Can the command be batched
                if (command.AddToBatch(_dialect, localQueries, localDbCommand, localActions, index))
                {
                    // Does it go over the page or parameters limits

                    var tooManyQueries = batch.Queries.Count + localQueries.Count > _store.Configuration.CommandsPageSize;
                    var tooManyCommands = batch.Command.Parameters.Count + localDbCommand.Parameters.Count > _store.Configuration.SqlDialect.MaxParametersPerCommand;

                    if (tooManyQueries || tooManyCommands)
                    {
                        batches.Add(batch);

                        // Then start a new batch
                        batch = new BatchCommand(_connection.CreateCommand());
                    }

                    // We can add the queries to the current batch
                    batch.Queries.AddRange(localQueries);
                    batch.Actions.AddRange(localActions);
                    for (var i = localDbCommand.Parameters.Count - 1; i >= 0; i--)
                    {
                        // npgsql will prevent a parameter from being added to a collection
                        // if it's already in another one
                        var parameter = localDbCommand.Parameters[i];
                        localDbCommand.Parameters.RemoveAt(i);
                        batch.Command.Parameters.Add(parameter);
                    }
                }
                else
                {
                    // The command can't be added to a batch, we leave it in the list of commands to execute individually

                    // Finalize the current batch
                    if (batch.Queries.Count > 0)
                    {
                        batches.Add(batch);

                        // Then start a new batch
                        batch = new BatchCommand(_connection.CreateCommand());
                    }

                    batches.Add(command);
                }

                localQueries.Clear();
                localDbCommand.Parameters.Clear();
                localActions.Clear();
            }

            // If the ongoing batch is not empty, add it
            if (batch.Queries.Count > 0)
            {
                batches.Add(batch);
            }

            _commands.Clear();
            _commands.AddRange(batches);
        }

        public void EnterAsyncExecution()
        {
            if (!_enableThreadSafetyChecks)
            {
                return;
            }

            if (Interlocked.Increment(ref _asyncOperations) > 1)
            {
                throw new InvalidOperationException($"Two concurrent threads have been detected accessing the same ISession instance from: \n{Environment.StackTrace}\nand:\n{_previousStackTrace}\n---");
            }

            _previousStackTrace = Environment.StackTrace;
        }

        public void ExitAsyncExecution()
        {
            if (!_enableThreadSafetyChecks)
            {
                return;
            }

            Interlocked.Decrement(ref _asyncOperations);
        }

        public async Task SaveChangesAsync()
        {
            EnterAsyncExecution();

            try
            {
                if (!_cancel)
                {
                    await FlushInternalAsync(true);

                    _save = true;
                }
            }
            finally
            {
                await CommitOrRollbackTransactionAsync();
                ExitAsyncExecution();
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Do nothing if Dispose() was already called
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                await CommitOrRollbackTransactionAsync();
            }
            catch
            {
                _transaction = null;
                _connection = null;
            }

            GC.SuppressFinalize(this);
        }

        private async Task CommitOrRollbackTransactionAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    if (_cancel || !_save)
                    {
                        await _transaction.RollbackAsync();

                        return;
                    }

                    await _transaction.CommitAsync();
                }
            }
            finally
            {
                await ReleaseConnectionAsync();
            }
        }

        /// <summary>
        /// Clears all the resources associated to the transaction.
        /// </summary>
        private async Task ReleaseTransactionAsync()
        {
            foreach (var state in _collectionStates.Values)
            {
                state._concurrent?.Clear();
                state._saved?.Clear();
                state._updated?.Clear();
                state._tracked?.Clear();
                state._deleted?.Clear();
                state._maps?.Clear();

                // Clear the identity map as we don't want to return stale data after committing some changes.
                // We assume the identity map is part of the unit-of-work.
                state._identityMap?.Clear();
            }

            _commands?.Clear();
            _commands = null;

            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        private async Task ReleaseConnectionAsync()
        {
            await ReleaseTransactionAsync();

            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        /// <summary>
        /// Clears all the resources associated to the transaction.
        /// </summary>
        private void ReleaseTransaction()
        {
            foreach (var state in _collectionStates.Values)
            {
                // IndentityMap is cleared in ReleaseSession()
                state._concurrent?.Clear();
                state._saved?.Clear();
                state._updated?.Clear();
                state._tracked?.Clear();
                state._deleted?.Clear();
                state._maps?.Clear();
            }

            _commands?.Clear();
            _commands = null;

            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        private void ReleaseConnection()
        {
            ReleaseTransaction();

            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        /// <summary>
        /// Whether the current session has data to flush or not.
        /// </summary>
        internal bool HasWork()
        {
            foreach (var state in _collectionStates.Values)
            {
                if (
                    state.Saved.Count +
                    state.Updated.Count +
                    state.Tracked.Count +
                    state.Deleted.Count > 0
                    )
                {
                    return true;
                }
            }

            return false;
        }

        private async Task ReduceAsync()
        {
            foreach (var collectionState in _collectionStates)
            {
                var state = collectionState.Value;
                var collection = collectionState.Key;

                // loop over each Indexer used by new objects
                foreach (var descriptor in state.Maps.Keys)
                {
                    // if the descriptor has no reduce behavior, ignore it
                    if (descriptor.Reduce == null)
                    {
                        continue;
                    }

                    if (descriptor.GroupKey == null)
                    {
                        throw new InvalidOperationException(
                            "A map/reduce index must declare at least one property with a GroupKey attribute: " +
                            descriptor.Type.FullName);
                    }

                    // a grouping method for the current descriptor
                    var descriptorGroup = GetGroupingMethod(descriptor);

                    // list all available grouping keys in the current set
                    var allKeysForDescriptor =
                        state.Maps[descriptor].Select(x => x.Map).Select(descriptorGroup).Distinct().ToList();

                    // reduce each group, will result in one Reduced index per group
                    foreach (var currentKey in allKeysForDescriptor)
                    {
                        // group all mapped indexes
                        var newMapsGroup =
                            state.Maps[descriptor].Where(x => x.State == MapStates.New).Select(x => x.Map).Where(
                                x => descriptorGroup(x).Equals(currentKey)).ToList();

                        var deletedMapsGroup =
                            state.Maps[descriptor].Where(x => x.State == MapStates.Delete).Select(x => x.Map).Where(
                                x => descriptorGroup(x).Equals(currentKey)).ToList();

                        var updatedMapsGroup =
                            state.Maps[descriptor].Where(x => x.State == MapStates.Update).Select(x => x.Map).Where(
                                x => descriptorGroup(x).Equals(currentKey)).ToList();

                        // todo: if an updated object got his Key changed, then apply a New to the new value group
                        // and a Delete to the old value group. Otherwise apply Update to the current value group

                        IIndex index = null;

                        if (newMapsGroup.Count > 0)
                        {
                            // reducing an already grouped set (technically the reduction should contain the grouping step, but by design ...)
                            index = descriptor.Reduce(newMapsGroup.GroupBy(descriptorGroup).First());

                            if (index == null)
                            {
                                throw new InvalidOperationException(
                                    "The reduction on a grouped set should have resulted in a unique result"
                                    );
                            }
                        }

                        var dbIndex = await ReduceForAsync(descriptor, currentKey, collection);

                        // if index present in db and new objects, reduce them
                        if (dbIndex != null && index != null)
                        {
                            // reduce over the two objects
                            var reductions = new[] { dbIndex, index };

                            var groupedReductions = reductions.GroupBy(descriptorGroup).SingleOrDefault()
                                ?? throw new InvalidOperationException("The grouping on the db and in memory set should have resulted in a unique result");

                            index = descriptor.Reduce(groupedReductions);

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
                            if (deletedMapsGroup.Count > 0)
                            {
                                index = descriptor.Delete(index, deletedMapsGroup.GroupBy(descriptorGroup).First());
                                // At this point, index can be null if the reduction returned a null index from Delete handler
                            }

                            // are there any updated object for this descriptor/group ?
                            if (updatedMapsGroup.Count > 0)
                            {
                                index = descriptor.Update(index, updatedMapsGroup.GroupBy(descriptorGroup).First());
                            }
                        }

                        var deletedDocumentIds = deletedMapsGroup.SelectMany(x => x.GetRemovedDocuments().Select(d => d.Id)).ToList();
                        var addedDocumentIds = newMapsGroup.SelectMany(x => x.GetAddedDocuments().Select(d => d.Id)).ToList();

                        _commands ??= new List<IIndexCommand>();

                        if (dbIndex != null)
                        {
                            if (index == null)
                            {
                                _commands.Add(new DeleteReduceIndexCommand(dbIndex, Store, collection));
                            }
                            else
                            {
                                index.Id = dbIndex.Id;

                                var common = addedDocumentIds.Intersect(deletedDocumentIds).ToList();
                                addedDocumentIds = addedDocumentIds.Where(x => !common.Contains(x)).ToList();
                                deletedDocumentIds = deletedDocumentIds.Where(x => !common.Contains(x)).ToList();

                                // Update updated, new and deleted linked documents
                                _commands.Add(new UpdateIndexCommand(index, addedDocumentIds, deletedDocumentIds, Store, collection));
                            }
                        }
                        else
                        {
                            if (index != null)
                            {
                                // The index is new
                                _commands.Add(new CreateIndexCommand(index, addedDocumentIds, Store, collection));
                            }
                        }
                    }
                }
            }
        }

        private async Task<ReduceIndex> ReduceForAsync(IndexDescriptor descriptor, object currentKey, string collection)
        {
            await CreateConnectionAsync();

            var name = _tablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(descriptor.IndexType, collection);
            var sql = "select * from " + _dialect.QuoteForTableName(name, _store.Configuration.Schema) + " where " + _dialect.QuoteForColumnName(descriptor.GroupKey.Name) + " = @currentKey";

            var index = await _connection.QueryAsync(descriptor.IndexType, sql, new { currentKey }, _transaction);
            return index.FirstOrDefault() as ReduceIndex;
        }

        /// <summary>
        /// Creates a Func{IIndex, object}; dynamically, based on GroupKey attributes
        /// this function will be used as the keySelector for Linq.Grouping
        /// </summary>
        private Func<IIndex, object> GetGroupingMethod(IndexDescriptor descriptor)
        {
            return _store.GroupMethods.GetOrAdd(descriptor.Type, type =>
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
        private async Task<IEnumerable<IndexDescriptor>> GetDescriptorsAsync(Type t, string collection)
        {
            _descriptors ??= new Dictionary<string, IEnumerable<IndexDescriptor>>();

            var cacheKey = string.IsNullOrEmpty(collection)
                ? t.FullName
                : string.Concat(t.FullName + ":" + collection)
                ;

            if (!_descriptors.TryGetValue(cacheKey, out var typedDescriptors))
            {
                typedDescriptors = _store.Describe(t, collection);

                if (_indexes != null)
                {
                    typedDescriptors = typedDescriptors.Union(_store.CreateDescriptors(t, collection, _indexes)).ToArray();
                }

                _descriptors.Add(cacheKey, typedDescriptors);
            }

            if (BuildExtraIndexDescriptors != null)
            {
                var dynamicIndexDes = await BuildExtraIndexDescriptors(t, collection);
                if (dynamicIndexDes != null)
                {
                    return typedDescriptors.Union(ExtraIndexDescriptors.Union(dynamicIndexDes));
                }
            }

            return typedDescriptors.Union(ExtraIndexDescriptors);
        }
        private async Task MapNew(Document document, object obj, string collection)
        {
            var descriptors = await GetDescriptorsAsync(obj.GetType(), collection);

            var state = GetState(collection);

            foreach (var descriptor in descriptors)
            {
                // Ignore index if the object is filtered out
                if (descriptor.Filter != null && !descriptor.Filter.Invoke(obj))
                {
                    continue;
                }

                var mapped = await descriptor.Map(obj);

                if (mapped != null)
                {
                    foreach (var index in mapped)
                    {
                        if (index == null)
                        {
                            continue;
                        }

                        _commands ??= new List<IIndexCommand>();

                        index.AddDocument(document);

                        // if the mapped elements are not meant to be reduced,
                        // then save them in db, as index
                        if (descriptor.Reduce == null)
                        {
                            if (index.Id == 0)
                            {
                                _commands.Add(new CreateIndexCommand(index, Enumerable.Empty<long>(), Store, collection));
                            }
                            else
                            {
                                _commands.Add(new UpdateIndexCommand(index, Enumerable.Empty<long>(), Enumerable.Empty<long>(), Store, collection));
                            }
                        }
                        else
                        {
                            // save for later reducing
                            if (!state.Maps.TryGetValue(descriptor, out var mapStates))
                            {
                                state.Maps.Add(descriptor, mapStates = new List<MapState>());
                            }

                            mapStates.Add(new MapState(index, MapStates.New));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update map and reduce indexes when an entity is deleted.
        /// </summary>
        private async Task MapDeleted(Document document, object obj, string collection)
        {
            var descriptors = await GetDescriptorsAsync(obj.GetType(), collection);

            var state = GetState(collection);

            foreach (var descriptor in descriptors)
            {
                // Ignore index if the object is filtered out
                if (descriptor.Filter != null && !descriptor.Filter.Invoke(obj))
                {
                    continue;
                }

                _commands ??= new List<IIndexCommand>();

                // If the mapped elements are not meant to be reduced, delete
                if (descriptor.Reduce == null || descriptor.Delete == null)
                {
                    _commands.Add(new DeleteMapIndexCommand(descriptor.IndexType, document.Id, Store, collection));
                }
                else
                {
                    var mapped = await descriptor.Map(obj);

                    if (mapped != null)
                    {
                        foreach (var index in mapped)
                        {
                            // save for later reducing
                            if (!state.Maps.TryGetValue(descriptor, out var mapStates))
                            {
                                state.Maps.Add(descriptor, mapStates = new List<MapState>());
                            }

                            mapStates.Add(new MapState(index, MapStates.Delete));
                            index.RemoveDocument(document);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new connection if none has been yet. Use this method when reads need to be done.
        /// </summary>
        public async Task<DbConnection> CreateConnectionAsync()
        {
            CheckDisposed();

            _connection ??= _store.Configuration.ConnectionFactory.CreateConnection();

            if (_connection.State == ConnectionState.Closed)
            {
                await _connection.OpenAsync();
            }

            return _connection;
        }

        public DbTransaction CurrentTransaction => _transaction;

        public Task<DbTransaction> BeginTransactionAsync()
            => BeginTransactionAsync(Store.Configuration.IsolationLevel);

        /// <summary>
        /// Begins a new transaction if none has been yet. Use this method when writes need to be done.
        /// </summary>
        public async Task<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            CheckDisposed();

            if (_transaction == null)
            {
                await CreateConnectionAsync();

                // In the case of shared connections (InMemory) this can throw as the transaction
                // might already be set by a concurrent thread on the same shared connection.
                _transaction = await _connection.BeginTransactionAsync(isolationLevel);
            }

            return _transaction;
        }

        public Task CancelAsync()
        {
            EnterAsyncExecution();

            try
            {
                CheckDisposed();

                _cancel = true;

                return ReleaseTransactionAsync();
            }
            finally
            {
                ExitAsyncExecution();
            }
        }

        public IStore Store => _store;
    }
}
