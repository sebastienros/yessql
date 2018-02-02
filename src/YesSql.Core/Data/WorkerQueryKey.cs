// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace YesSql.Data
{
    /// <summary>
    /// An instance of <see cref="WorkerQueryKey"/> represents the state of <see cref="WorkerQueryKey"/>.
    /// </summary>
    public class WorkerQueryKey : IEquatable<WorkerQueryKey>
    {
        private readonly string _prefix;
        private readonly int[] _ids;
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
            _ids = ids;
        }

        public WorkerQueryKey(string prefix, Dictionary<string, object> parameters)
        {
            _prefix = prefix;
            _parameters = parameters;
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
            return string.Equals(other._prefix, _prefix, StringComparison.Ordinal) &&
                AreSame(_ids, other._ids) &&
                AreSame(_parameters, other._parameters)
                ;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Caching as the key is immutable and it can be called
            // multiple times during a request.
            if (!_hashcode.HasValue)
            {

                var combinedHash = 5381;
                combinedHash = ((combinedHash << 5) + combinedHash) ^ _prefix.GetHashCode();

                if (_ids != null)
                {
                    foreach (var id in _ids)
                    {
                        combinedHash = ((combinedHash << 5) + combinedHash) ^ id;
                    }
                }

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
                }

                _hashcode = combinedHash;
            }

            return _hashcode.Value;
        }
        
        private static bool AreSame(Dictionary<string, object> values1, Dictionary<string, object> values2)
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

            while (enumerator1.MoveNext() && enumerator2.MoveNext())
            {
                
                if (!string.Equals(enumerator1.Current.Key, enumerator2.Current.Key, StringComparison.Ordinal) ||
                    enumerator1.Current.Value != enumerator2.Current.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreSame(IList<int> values1, IList<int> values2)
        {
            if (values1 == values2)
            {
                return true;
            }

            if (values1 == null || values2 == null || values1.Count != values2.Count)
            {
                return false;
            }

            for (var i = 0; i < values1.Count; i++)
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