using Dapper;
using System;
using System.Collections;
using System.Data;
using System.Linq;

namespace YesSql.Data
{
    /// <summary>
    /// A parameter whose value is an array, for databases accepting arrays as parameter values (see <see cref="Provider.IArrayParameterDialect"/>).
    /// </summary>
    /// <remarks>
    /// The parameter is created directly instead of letting Dapper handle the array, which Dapper expands into one
    /// parameter per value unless it recognizes the connection as one supporting arrays. Equality is structural so that
    /// queries with the same values can share their execution when query gating is enabled.
    /// </remarks>
    internal sealed class ArrayParameter : SqlMapper.ICustomQueryParameter, IEquatable<ArrayParameter>
    {
        public ArrayParameter(Array values)
        {
            Values = values;
        }

        public Array Values { get; }

        public void AddParameter(IDbCommand command, string name)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = Values;
            command.Parameters.Add(parameter);
        }

        public bool Equals(ArrayParameter other)
            => other is not null && StructuralComparisons.StructuralEqualityComparer.Equals(Values, other.Values);

        public override bool Equals(object obj) => Equals(obj as ArrayParameter);

        public override int GetHashCode() => StructuralComparisons.StructuralEqualityComparer.GetHashCode(Values);

        public override string ToString() => "[" + string.Join(", ", Values.Cast<object>()) + "]";
    }
}
