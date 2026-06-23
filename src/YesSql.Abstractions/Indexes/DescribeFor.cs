using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace YesSql.Indexes
{
    /// <summary>
    /// Describes how an index is mapped, grouped, reduced and deleted for a document type.
    /// </summary>
    public interface IDescribeFor
    {
        /// <summary>
        /// Gets the delegate that maps a document to a set of indexes.
        /// </summary>
        /// <returns>A delegate producing the indexes for a given document.</returns>
        Func<object, CancellationToken, Task<IEnumerable<IIndex>>> GetMap();

        /// <summary>
        /// Gets the delegate that reduces a group of indexes into a single index.
        /// </summary>
        /// <returns>A delegate reducing a grouping of indexes, or <c>null</c> when no reduction is defined.</returns>
        Func<IGrouping<object, IIndex>, IIndex> GetReduce();

        /// <summary>
        /// Gets the delegate that computes the index to delete from a reduced group.
        /// </summary>
        /// <returns>A delegate producing the index to delete, or <c>null</c> when no deletion is defined.</returns>
        Func<IIndex, IEnumerable<IIndex>, IIndex> GetDelete();

        /// <summary>
        /// Gets or sets the property used to group indexes when reducing.
        /// </summary>
        PropertyInfo GroupProperty { get; set; }

        /// <summary>
        /// Gets the type of the index that is described.
        /// </summary>
        Type IndexType { get; }

        /// <summary>
        /// Gets the predicate used to filter the documents that are mapped, or <c>null</c> when no filter is defined.
        /// </summary>
        Func<object, bool> Filter { get; }
    }

    /// <summary>
    /// Configures the mapping of a document of type <typeparamref name="T"/> to an index of type <typeparamref name="TIndex"/>.
    /// </summary>
    /// <typeparam name="T">The type of the document to map.</typeparam>
    /// <typeparam name="TIndex">The type of the index to produce.</typeparam>
    public interface IMapFor<out T, TIndex> where TIndex : IIndex
    {
        /// <summary>
        /// Maps a document to a single index.
        /// </summary>
        /// <param name="map">A delegate producing an index from a document.</param>
        /// <returns>An <see cref="IGroupFor{TIndex}"/> used to optionally group the index.</returns>
        IGroupFor<TIndex> Map(Func<T, TIndex> map);

        /// <summary>
        /// Maps a document to a set of indexes.
        /// </summary>
        /// <param name="map">A delegate producing indexes from a document.</param>
        /// <returns>An <see cref="IGroupFor{TIndex}"/> used to optionally group the indexes.</returns>
        IGroupFor<TIndex> Map(Func<T, IEnumerable<TIndex>> map);

        /// <summary>
        /// Maps a document to a single index asynchronously.
        /// </summary>
        /// <param name="map">A delegate asynchronously producing an index from a document.</param>
        /// <returns>An <see cref="IGroupFor{TIndex}"/> used to optionally group the index.</returns>
        IGroupFor<TIndex> Map(Func<T, Task<TIndex>> map);

        /// <summary>
        /// Maps a document to a single index asynchronously.
        /// </summary>
        /// <param name="map">A delegate asynchronously producing an index from a document, honoring a cancellation token.</param>
        /// <returns>An <see cref="IGroupFor{TIndex}"/> used to optionally group the index.</returns>
        IGroupFor<TIndex> Map(Func<T, CancellationToken, Task<TIndex>> map);

        /// <summary>
        /// Maps a document to a set of indexes asynchronously.
        /// </summary>
        /// <param name="map">A delegate asynchronously producing indexes from a document.</param>
        /// <returns>An <see cref="IGroupFor{TIndex}"/> used to optionally group the indexes.</returns>
        IGroupFor<TIndex> Map(Func<T, Task<IEnumerable<TIndex>>> map);

        /// <summary>
        /// Maps a document to a set of indexes asynchronously.
        /// </summary>
        /// <param name="map">A delegate asynchronously producing indexes from a document, honoring a cancellation token.</param>
        /// <returns>An <see cref="IGroupFor{TIndex}"/> used to optionally group the indexes.</returns>
        IGroupFor<TIndex> Map(Func<T, CancellationToken, Task<IEnumerable<TIndex>>> map);

        /// <summary>
        /// Restricts the mapping to the documents that satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate a document must satisfy to be mapped.</param>
        /// <returns>The current <see cref="IMapFor{T, TIndex}"/> instance so that calls can be chained.</returns>
        IMapFor<T, TIndex> When(Func<T, bool> predicate);
    }

    /// <summary>
    /// Configures how mapped indexes of type <typeparamref name="TIndex"/> are grouped before being reduced.
    /// </summary>
    /// <typeparam name="TIndex">The type of the index to group.</typeparam>
    public interface IGroupFor<TIndex> where TIndex : IIndex
    {
        /// <summary>
        /// Groups the mapped indexes by the specified key.
        /// </summary>
        /// <typeparam name="TKey">The type of the grouping key.</typeparam>
        /// <param name="group">An expression selecting the property used as the grouping key.</param>
        /// <returns>An <see cref="IReduceFor{TIndex, TKey}"/> used to configure the reduction.</returns>
        IReduceFor<TIndex, TKey> Group<TKey>(Expression<Func<TIndex, TKey>> group);
    }

    /// <summary>
    /// Configures how a group of indexes of type <typeparamref name="TIndex"/> is reduced into a single index.
    /// </summary>
    /// <typeparam name="TIndex">The type of the index to reduce.</typeparam>
    /// <typeparam name="TKey">The type of the grouping key.</typeparam>
    public interface IReduceFor<TIndex, out TKey> where TIndex : IIndex
    {
        /// <summary>
        /// Reduces a group of indexes into a single index.
        /// </summary>
        /// <param name="reduce">A delegate producing the reduced index from a grouping.</param>
        /// <returns>An <see cref="IDeleteFor{TIndex}"/> used to configure how the reduced index is deleted.</returns>
        IDeleteFor<TIndex> Reduce(Func<IGrouping<TKey, TIndex>, TIndex> reduce);
    }

    /// <summary>
    /// Configures how a reduced index of type <typeparamref name="TIndex"/> is updated when documents are removed.
    /// </summary>
    /// <typeparam name="TIndex">The type of the index being deleted.</typeparam>
    public interface IDeleteFor<TIndex> where TIndex : IIndex
    {
        /// <summary>
        /// Defines how a reduced index is recomputed when documents are removed from its group.
        /// </summary>
        /// <param name="delete">A delegate producing the updated index, or <c>null</c> to remove the index entirely.</param>
        void Delete(Func<TIndex, IEnumerable<TIndex>, TIndex> delete = null);
    }

    /// <summary>
    /// Default implementation of the index description fluent interfaces, capturing the map,
    /// group, reduce and delete delegates for an index of type <typeparamref name="TIndex"/>.
    /// </summary>
    /// <typeparam name="T">The type of the document to map.</typeparam>
    /// <typeparam name="TIndex">The type of the index to produce.</typeparam>
    /// <typeparam name="TKey">The type of the grouping key.</typeparam>
    public class IndexDescriptor<T, TIndex, TKey> : IDescribeFor, IMapFor<T, TIndex>, IGroupFor<TIndex>, IReduceFor<TIndex, TKey>, IDeleteFor<TIndex> where TIndex : IIndex
    {
        private Func<T, CancellationToken, Task<IEnumerable<TIndex>>> _map;
        private Func<IGrouping<TKey, TIndex>, TIndex> _reduce;
        private Func<TIndex, IEnumerable<TIndex>, TIndex> _delete;
        private IDescribeFor _reduceDescribeFor;
        private Func<object, bool> _filter;

        /// <inheritdoc/>
        public PropertyInfo GroupProperty { get; set; }

        /// <inheritdoc/>
        public Type IndexType => typeof(TIndex);

        /// <inheritdoc/>
        public Func<object, bool> Filter => _filter;

        /// <inheritdoc/>
        public IGroupFor<TIndex> Map(Func<T, IEnumerable<TIndex>> map)
        {
            _map = (x, token) => Task.FromResult(map(x));
            return this;
        }

        /// <inheritdoc/>
        public IMapFor<T, TIndex> When(Func<T, bool> predicate)
        {
            _filter = x => predicate((T)x);
            return this;
        }

        /// <inheritdoc/>
        public IGroupFor<TIndex> Map(Func<T, TIndex> map)
        {
            _map = (x, token) => Task.FromResult((IEnumerable<TIndex>)new[] { map(x) });
            return this;
        }

        /// <inheritdoc/>
        public IGroupFor<TIndex> Map(Func<T, Task<IEnumerable<TIndex>>> map)
        {
            _map = async (x,token) => await map(x);
            return this;
        }

        /// <inheritdoc/>
        public IGroupFor<TIndex> Map(Func<T, CancellationToken, Task<IEnumerable<TIndex>>> map)
        {
            _map = map;
            return this;
        }

        /// <inheritdoc/>
        public IGroupFor<TIndex> Map(Func<T,  Task<TIndex>> map)
        {
            _map = async (x, token) => new[] { await map(x) };
            return this;
        }

        /// <inheritdoc/>
        public IGroupFor<TIndex> Map(Func<T, CancellationToken, Task<TIndex>> map)
        {
            _map = async (x, token) => new[] { await map(x, token) };
            return this;
        }


        /// <inheritdoc/>
        public IReduceFor<TIndex, TKeyG> Group<TKeyG>(Expression<Func<TIndex, TKeyG>> group)
        {
            var memberExpression = group.Body as MemberExpression
                ?? throw new ArgumentException("Group expression is not a valid member of: " + typeof(TIndex).Name);

            var property = memberExpression.Member as PropertyInfo
                ?? throw new ArgumentException("Group expression is not a valid property of: " + typeof(TIndex).Name);

            GroupProperty = property;

            var reduceDescribeFor = new IndexDescriptor<T, TIndex, TKeyG>();
            _reduceDescribeFor = reduceDescribeFor;

            return reduceDescribeFor;
        }

        /// <inheritdoc/>
        public IDeleteFor<TIndex> Reduce(Func<IGrouping<TKey, TIndex>, TIndex> reduce)
        {
            _reduce = reduce;
            return this;
        }

        /// <inheritdoc/>
        public void Delete(Func<TIndex, IEnumerable<TIndex>, TIndex> delete = null)
        {
            _delete = delete;
        }

        Func<object, CancellationToken, Task<IEnumerable<IIndex>>> IDescribeFor.GetMap()
        {
            return async (x, token) => (await _map((T)x, token) ?? Enumerable.Empty<TIndex>()).Cast<IIndex>();
        }

        Func<IGrouping<object, IIndex>, IIndex> IDescribeFor.GetReduce()
        {
            if (_reduceDescribeFor != null)
            {
                return _reduceDescribeFor.GetReduce();
            }

            if (_reduce == null)
            {
                return null;
            }

            return x =>
            {
                var grouping = new GroupedEnumerable<TKey, TIndex>(x.Key, x);
                return _reduce(grouping);
            };
        }

        Func<IIndex, IEnumerable<IIndex>, IIndex> IDescribeFor.GetDelete()
        {
            if (_reduceDescribeFor != null)
            {
                return _reduceDescribeFor.GetDelete();
            }

            return (index, obj) => _delete((TIndex)index, obj.Cast<TIndex>());
        }
    }

    /// <summary>
    /// Represents a grouping of indexes of type <typeparamref name="TIndex"/> sharing the same key.
    /// </summary>
    /// <typeparam name="TKey">The type of the grouping key.</typeparam>
    /// <typeparam name="TIndex">The type of the grouped indexes.</typeparam>
    public class GroupedEnumerable<TKey, TIndex> : IGrouping<TKey, TIndex> where TIndex : IIndex
    {
        private readonly object _key;
        private readonly IEnumerable<IIndex> _enumerable;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupedEnumerable{TKey, TIndex}"/> class.
        /// </summary>
        /// <param name="key">The key shared by the indexes in the group.</param>
        /// <param name="enumerable">The indexes that belong to the group.</param>
        public GroupedEnumerable(object key, IEnumerable<IIndex> enumerable)
        {
            _key = key;
            _enumerable = enumerable;
        }

        /// <summary>
        /// Gets the key shared by all the indexes in the group.
        /// </summary>
        public TKey Key => (TKey)_key;

        /// <summary>
        /// Returns an enumerator that iterates through the indexes in the group.
        /// </summary>
        /// <returns>An enumerator for the grouped indexes.</returns>
        public IEnumerator<TIndex> GetEnumerator()
        {
            return _enumerable.Cast<TIndex>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}