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
        private readonly int[] _ids;
        private readonly Dictionary<string, object> _parameters;
        private readonly int _hashcode;

        public WorkerQueryKey(string prefix, int[] ids)
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
            if (_parameters != null)
            {
                return string.Equals(other._prefix, _prefix, StringComparison.Ordinal) &&
                SameParameters(_parameters, other._parameters)
                ;
            }
            else
            {
                return String.Equals(_prefix, other._prefix, StringComparison.Ordinal);
            }
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
                    combinedHash = ((combinedHash << 5) + combinedHash) ^ id;
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

            var enumerator1 = values1.GetEnumerator();
            var enumerator2 = values2.GetEnumerator();

            while (true)
            {
                var hasMore1 = enumerator1.MoveNext();
                var hasMore2 = enumerator2.MoveNext();

                if (!hasMore1 && !hasMore2)
                {
                    return true;
                }

                if (!hasMore1 || !hasMore2)
                {
                    return false;
                }

                var current1 = enumerator1.Current;
                var current2 = enumerator2.Current;

                if (!string.Equals(current1.Key, current2.Key, StringComparison.Ordinal) ||
                    current1.Value != current2.Value)
                {
                    return false;
                }
            }
        }

        private static bool SameIds(int[] values1, int[] values2)
        {
            if (values1 == values2)
            {
                return true;
            }

            if ((values1 == null && values2 != null) || (values1 != null && values2 == null) || values1.Length != values2.Length)
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
