using System;
using System.Collections.Generic;

namespace YesSql.Data
{
    /// <summary>
    /// An instance of <see cref="WorkerQueryKey"/> represents the state of <see cref="WorkerQueryKey"/>.
    /// </summary>
    public readonly struct WorkerQueryKey : IEquatable<WorkerQueryKey>
    {
        private readonly string _prefix;
        private readonly long[] _ids;
        private readonly Dictionary<string, object> _parameters;
        private readonly int _hashcode;

        public WorkerQueryKey(string prefix, long[] ids)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            _prefix = prefix;
            _parameters = null;
            _ids = ids;
            _hashcode = 0;
            _hashcode = BuildHashCode();
        }

        public WorkerQueryKey(string prefix, Dictionary<string, object> parameters)
        {
            _prefix = prefix;
            _parameters = parameters;
            _ids = null;
            _hashcode = 0;
            _hashcode = BuildHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is WorkerQueryKey other)
            {
                return Equals(other);
            }

            return false;
        }

        /// <inheritdoc />
        public bool Equals(WorkerQueryKey other)
        {
            if (!string.Equals(other._prefix, _prefix, StringComparison.Ordinal))
            {
                return false;
            }
            
            if (_parameters != null || other._parameters != null)
            {
                return SameParameters(_parameters, other._parameters);
            }
            
            if (_ids != null || other._ids != null)
            {
                return SameIds(_ids, other._ids);
            }

            return true;
        }

        private int BuildHashCode()
        {
            var combinedHash = 5381;
            combinedHash = ((combinedHash << 5) + combinedHash) ^ _prefix.GetHashCode();


            if (_parameters != null)
            {
                foreach (var parameter in _parameters)
                {
                    if (parameter.Key != null)
                    {
                        combinedHash = ((combinedHash << 5) + combinedHash) ^ parameter.Key.GetHashCode();
                    }

                    if (parameter.Value != null)
                    {
                        combinedHash = ((combinedHash << 5) + combinedHash) ^ parameter.Value.GetHashCode();
                    }
                }

                return combinedHash;
            }

            if (_ids != null)
            {
                foreach (var id in _ids)
                {
                    combinedHash = ((combinedHash << 5) + combinedHash) ^ id.ToString().GetHashCode();
                }

                return combinedHash;
            }

            return default;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _hashcode;
        }

        private static bool SameParameters(Dictionary<string, object> values1, Dictionary<string, object> values2)
        {
            if (values1 == values2)
            {
                return true;
            }

            if ((values1 == null && values2 != null) || (values1 != null && values2 == null) || values1.Count != values2.Count)
            {
                return false;
            }

            foreach (var entry1 in values1)
            {
                var key1 = entry1.Key;

                if (!values2.TryGetValue(key1, out var value2))
                {
                    return false;
                }

                var value1 = entry1.Value;

                if (value1 == null)
                {
                    if (value2 != null)
                    {
                        return false;
                    }
                }
                else
                {
                    if (!value1.Equals(value2))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool SameIds(long[] values1, long[] values2)
        {
            // If one is not null both need to be non-null
            if (!(values1 != null && values2 != null))
            {
                return false;
            }

            if (values1.Length != values2.Length)
            {
                return false;
            }

            for (var i = 0; i < values1.Length; i++)
            {
                if (values1[i] != values2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
