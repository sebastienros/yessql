using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Criterion;
using YesSql.Core.Data.Models;
using YesSql.Core.Indexes;
using YesSql.Core.Query;
using YesSql.Core.Serialization;
using NHibernate.Linq;
using Expression = System.Linq.Expressions.Expression;
using ISession = YesSql.Core.Services.ISession;
using System.Data;

namespace YesSql.Core.Data
{
    public class Session : ISession
    {
        private readonly NHibernate.ISession _session;
        private readonly Store _store;
        private readonly IDocumentSerializer _serializer;
        private readonly IDictionary<IndexDescriptor, IList<MapState>> _maps;
        private readonly IDictionary<object, int> _documents = new Dictionary<object, int>();
        private readonly HashSet<object> _saved = new HashSet<object>();
        private readonly HashSet<object> _deleted = new HashSet<object>();
        private bool _cancel;

        // a dictionary of returned objects indexed by document id
        private readonly IDictionary<int, object> _identityMap = new Dictionary<int, object>();

        public Session(NHibernate.ISession session, Store store)
        {
            _session = session;
            _store = store;
            _serializer = _store.GetDocumentSerializer();
            _maps = new Dictionary<IndexDescriptor, IList<MapState>>();
        }

        public void Cancel()
        {
            _cancel = true;
        }

        public void Dispose()
        {
            if (!_cancel)
            {
                // execute pending commands
                Flush();
            }

            _session.Dispose();
        }

        public void Save(Document document)
        {
            _saved.Add(document);
        }

        public void Save(object obj)
        {
            // don't add the object to the saved list if it's already tracked
            if (!_documents.ContainsKey(obj))
            {
                _saved.Add(obj);
            }
        }

        private void SaveConcrete(object obj)
        {
            if (obj is Document)
            {
                _session.Save(obj);
            }
            else if (obj is IIndex)
            {
                _session.Save(obj);
            }
            else
            {
                var doc = new Document();

                // convert the custom object to a storable document
                _serializer.Serialize(obj, ref doc);

                // if the object is not new, reload to get the old map
                int id;
                if (_documents.TryGetValue(obj, out id))
                {
                    var oldDoc = _session.Get<Document>(id);
                    var oldObj = _serializer.Deserialize(oldDoc);

                    // do nothing if the document hasn't been modified
                    if (oldDoc.Content != doc.Content)
                    {
                        oldDoc.Content = doc.Content;

                        MapDeleted(oldDoc, oldObj);

                        // update document
                        MapNew(oldDoc, obj);
                    }
                }
                else
                {
                    // new document
                    _session.Save(doc);

                    var accessor = _store.GetIdAccessor(obj.GetType(), "Id");

                    // if the object has an Id property, set it back
                    var ident = accessor.Get(obj);
                    if (ident != null)
                    {
                        accessor.Set(obj, doc.Id);
                    }

                    // track the newly created object
                    TrackObject(obj, doc);

                    MapNew(doc, obj);
                }
            }
        }

        private void TrackObject(object obj, Document doc)
        {
            // track object and document
            _documents.Add(obj, doc.Id);
            _identityMap.Add(doc.Id, obj);
        }

        private void UntrackObject(object obj, Document doc)
        {
            // untrack object and document
            _documents.Remove(obj);
            _identityMap.Remove(doc.Id);
        }

        public void Delete(Document document)
        {
            _deleted.Add(document);
        }
        
        public void Delete(object obj)
        {
            _deleted.Add(obj);            
        }

        private void DeleteConcrete(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            if (obj is Document)
            {
                Delete((Document)obj);
            }
            else if (obj is IIndex)
            {
                _session.Delete(obj);
            }
            else
            {
                // if the document has an Id property, use it to find the document
                var idInfo = obj.GetType().GetProperty("Id");
                if (idInfo == null)
                {
                    throw new InvalidOperationException("Could not delete object as it doesn't have an Id property");
                }

                var id = idInfo.GetValue(obj, null);
                var doc = _session.Get<Document>(id);

                if (doc != null)
                {
                    _session.Delete(doc);

                    // untrack the deleted object
                    UntrackObject(obj, doc);

                    MapDeleted(doc, obj);
                }
            }
        }

        public Document Get(int id)
        {
            return _session.Get<Document>(id);
        }

        public T Get<T>(int id) where T : class
        {
            return As<T>(_session.Get<Document>(id));
        }

        public IQueryable<Document> Load()
        {
            return _session.Query<Document>();
        }

        public IEnumerable<T> Load<T>(Func<IQueryable<Document>, IEnumerable<Document>> query) where T : class
        {
            var typeName = typeof (T).SimplifiedTypeName();
            var filter = _session.Query<Document>().Where(x => x.Type == typeName);

            if (query != null)
            {
                return As<T>(query(filter).ToList());
            }

            return As<T>(filter.ToList());
        }

        public T Load<T>(Func<IQueryable<Document>, Document> query) where T : class
        {
            var typeName = typeof (T).SimplifiedTypeName();
            var queried = query(_session.Query<Document>().Where(x => x.Type == typeName));
            return As<T>(queried);
        }
        
        public IQueryable<TIndex> QueryIndex<TIndex>() where TIndex : IIndex
        {
            return _session.Query<TIndex>();
        }

        public Query.IQuery Query()
        {
            return new DefaultQuery(_session, this);
        }

        public T As<T>(Document doc) where T : class
        {
            if (doc == null)
            {
                return null;
            }

            object cached;
            if (_identityMap.TryGetValue(doc.Id, out cached))
            {
                return (T) cached;
            }

            var obj = _serializer.Deserialize(doc) as T;

            if (obj == null)
            {
                throw new ArgumentException("Document of type '" + doc.Type + "' cannot be deserialized as '" +
                                            typeof (T).Name + "'");
            }

            // if the document has an Id property, set it back
            _store.GetIdAccessor(typeof(T), "Id").Set(obj, doc.Id);

            // track the loaded object
            TrackObject(obj, doc);

            return obj;
        }

        public IEnumerable<T> As<T>(IEnumerable<Document> doc) where T : class
        {
            return doc.Select(As<T>);
        }

        public Task CommitAsync()
        {
            return Task.Factory.StartNew(Reduce).ContinueWith(task => Dispose());
        }

        public void Flush()
        {
            using (var transaction = _session.BeginTransaction())
            {
                // saving all pending objects
                foreach (var obj in _documents.Keys)
                {
                    if (!_deleted.Contains(obj))
                    {
                        SaveConcrete(obj);
                    }
                }

                // saving all pending objects
                foreach (var obj in _saved)
                {
                    SaveConcrete(obj);
                }
                _saved.Clear();

                // deleting all pending objects
                foreach (var obj in _deleted)
                {
                    DeleteConcrete(obj);
                }
                _deleted.Clear();

                // compute all reduce indexes
                Reduce();

                // commit the transaction
                transaction.Commit();
            }
        }

        private void Reduce()
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
                                "The reduction on a grouped set shoud have resulted in a unique result");
                        }
                    }

                    // reduce in-database objects
                    var criterion = Restrictions.Eq(descriptor.GroupKey.Name, currentKey);

                    IIndex alias = null; // required by nhibernate
                    var dbIndex = _session
                        .QueryOver(descriptor.IndexType.Name, () => alias)
                        .Where(criterion)
                        .SingleOrDefault();

                    // if index present in db and new objects, reduce them
                    if (dbIndex != null && index != null)
                    {
                        // reduce over the two objects
                        var reductions = new[] {dbIndex, index};

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

                    // are there any deleted object for this descriptor/group ?
                    if (deletedMapsGroup.Any())
                    {
                        index = descriptor.Delete(index, deletedMapsGroup.GroupBy(descriptorGroup).First());
                    }

                    // are there any updated object for this descriptor/group ?
                    if (updatedMapsGroup.Any())
                    {
                        index = descriptor.Update(index, updatedMapsGroup.GroupBy(descriptorGroup).First());
                    }

                    if (index != null)
                    {
                        foreach (var doc in newMapsGroup.SelectMany(x => x.Documents))
                        {
                            index.Documents.Add(doc);
                        }

                        foreach (var doc in deletedMapsGroup.SelectMany(x => x.Documents))
                        {
                            index.Documents.Remove(doc);
                        }
                    }

                    if (dbIndex != null)
                    {
                        // the index needs to be deleted
                        if (index == null)
                        {
                            _session.Delete(dbIndex);
                        }
                        else
                        {
                            // updating the database record with updated values
                            var metadata = _session.SessionFactory.GetClassMetadata(index.GetType());
                            var values = metadata.GetPropertyValues(index, EntityMode.Poco);
                            metadata.SetPropertyValues(dbIndex, values, EntityMode.Poco);

                            _session.Save(dbIndex);
                        }
                    }
                    else
                    {
                        // new index record
                        _session.Save(index);
                    }
                }
            }
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
                var instance = Expression.Parameter(typeof (IIndex), "i");
                // i => ((TIndex)i)
                var convertInstance = Expression.Convert(instance, descriptor.GroupKey.DeclaringType);
                // i => ((TIndex)i).{Property}
                var property = Expression.Property(convertInstance, descriptor.GroupKey);
                // i => (object)(((TIndex)i).{Property})
                var convert = Expression.Convert(property, typeof (object));

                return Expression.Lambda<Func<IIndex, object>>(convert, instance).Compile();
            });
        }

        /// <summary>
        /// Returns the available indexers for a specified type
        /// </summary>
        public IEnumerable<IndexDescriptor> Describe(Type target)
        {
            if (target == null)
            {
                throw new ArgumentNullException();
            }

            return _store.Descriptors.GetOrAdd(target, key =>
            {
                var contextType = typeof (DescribeContext<>).MakeGenericType(target);
                var context = Activator.CreateInstance(contextType) as IDescriptor;
                
                foreach (var provider in _store.Indexes)
                {
                    if (provider.ForType().IsAssignableFrom(target))
                    {
                        provider.Describe(context);
                    }
                }

                return context.Describe(new[] {target}).ToList();
            });
        }

        // todo: remove it when migrations are done
        public IDbConnection GetConnection()
        {
            return _session.Connection;
        }

        private void MapNew(Document doc, object obj)
        {
            foreach (var descriptor in Describe(obj.GetType()))
            {
                var mapped = descriptor.Map(obj);

                foreach (var index in mapped)
                {
                    // if the mapped elements are not meant to be reduced,
                    // then save them in db, as index
                    if (descriptor.Reduce == null)
                    {
                        index.Documents.Add(doc);
                        _session.Save(index);
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

                        index.Documents.Add(doc);
                    }
                }
            }
        }

        private void MapDeleted(Document doc, object obj)
        {
            foreach (var descriptor in Describe(obj.GetType()))
            {
                // if the mapped elements are not meant to be reduced, ignore
                if (descriptor.Reduce == null || descriptor.Delete == null)
                {
                    continue;
                }

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
                    index.Documents.Remove(doc);
                }
            }
        }
    }
}
