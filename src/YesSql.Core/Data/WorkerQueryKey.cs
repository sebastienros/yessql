using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace YesSql.Data
{
    /// <summary>
    /// An instance of <see cref="WorkerQueryKey"/> represents the state of <see cref="WorkerQueryKey"/>.
    /// </summary>
    public struct WorkerQueryKey : IEquatable<WorkerQueryKey>
    {
        private static readonly ThreadLocal<StringBuilder> _stringBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder());

        private readonly string _prefix;
        private readonly Dictionary<string, object> _parameters;

        private int? _hashcode;

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

            if (ids != null && ids.Length > 0)
            {
                var stringBuilder = _stringBuilder.Value;
                stringBuilder.Clear();
                stringBuilder.Append(_prefix);

                foreach (var id in ids)
                {
                    stringBuilder.Append(";").Append(id);
                }

                _prefix = stringBuilder.ToString();
            }

            _hashcode = default(int?);
            _parameters = null;
        }

        public WorkerQueryKey(string prefix, Dictionary<string, object> parameters)
        {
            _prefix = prefix;
            _parameters = parameters;
            _hashcode = default(int?);
            
            if (parameters.Count < 5 && parameters.All(x => x.Value is ValueType))
            {
                var stringBuilder = _stringBuilder.Value;
                stringBuilder.Clear();
                stringBuilder.Append(_prefix);

                foreach (var parameter in _parameters)
                {
                    stringBuilder.Append(";").Append(parameter.Key).Append("=").Append(parameter.Value);
                }

                _parameters = null;
                _prefix = stringBuilder.ToString();
            }
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

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Caching as the key is immutable and it can be called
            // multiple times during a request.
            if (!_hashcode.HasValue)
            {
                if (_parameters != null)
                {
                    var combinedHash = 5381;
                    combinedHash = ((combinedHash << 5) + combinedHash) ^ _prefix.GetHashCode();

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

                    _hashcode = combinedHash;
                }
                else
                {
                    _hashcode = _prefix.GetHashCode();
                }
            }

            return _hashcode.Value;
        }

        private static bool SameParameters(Dictionary<string, object> values1, Dictionary<string, object> values2)
        {
            if (values1 == values2)
            {
                return true;
            }

            if (values1 == null || values2 == null || values1.Count != values2.Count)
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
    }
}
