using System;
using System.Collections.Generic;
using System.Linq;

namespace YesSql.Indexes
{
    /// <summary>
    /// Provides the context used by an <see cref="IIndexProvider"/> to describe how
    /// indexes are mapped, grouped and reduced for documents of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the document the indexes are described for.</typeparam>
    public class DescribeContext<T> : IDescriptor
    {
        private readonly Dictionary<Type, List<IDescribeFor>> _describes = new Dictionary<Type, List<IDescribeFor>>();

        /// <summary>
        /// Builds the <see cref="IndexDescriptor"/> instances for the described indexes.
        /// </summary>
        /// <param name="types">The index types to describe, or an empty array to describe all of them.</param>
        /// <returns>The descriptors matching the requested index types.</returns>
        public IEnumerable<IndexDescriptor> Describe(params Type[] types)
        {
            return _describes
                .Where(kp => types == null || types.Length == 0 || types.Contains(kp.Key))
                .SelectMany(x => x.Value)
                .Select(kp => new IndexDescriptor
                {
                    Type = kp.IndexType,
                    Map = kp.GetMap(),
                    Reduce = kp.GetReduce(),
                    Delete = kp.GetDelete(),
                    GroupKey = kp.GroupProperty,
                    IndexType = kp.IndexType,
                    Filter = kp.Filter
                });
        }

        /// <summary>
        /// Starts describing how an index of type <typeparamref name="TIndex"/> is built from the document.
        /// </summary>
        /// <typeparam name="TIndex">The type of the index to map.</typeparam>
        /// <returns>An <see cref="IMapFor{T, TIndex}"/> used to configure the mapping.</returns>
        public IMapFor<T, TIndex> For<TIndex>() where TIndex : IIndex
        {
            return For<TIndex, object>();
        }

        /// <summary>
        /// Starts describing how an index of type <typeparamref name="TIndex"/> with a group key of
        /// type <typeparamref name="TKey"/> is built from the document.
        /// </summary>
        /// <typeparam name="TIndex">The type of the index to map.</typeparam>
        /// <typeparam name="TKey">The type of the key used to group reduce indexes.</typeparam>
        /// <returns>An <see cref="IMapFor{T, TIndex}"/> used to configure the mapping.</returns>
        public IMapFor<T, TIndex> For<TIndex, TKey>() where TIndex : IIndex
        {
            if (!_describes.TryGetValue(typeof(T), out var descriptors))
            {
                descriptors = _describes[typeof(T)] = new List<IDescribeFor>();
            }

            var describeFor = new IndexDescriptor<T, TIndex, TKey>();
            descriptors.Add(describeFor);

            return describeFor;
        }
    }
}
