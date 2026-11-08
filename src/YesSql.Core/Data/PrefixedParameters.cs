using Dapper;
using System;
using System.Collections.Generic;
using System.Data;

namespace YesSql.Data
{
    /// <summary>
    /// Passes parameters to Dapper while keeping the '@' prefix of their names.
    /// </summary>
    /// <remarks>
    /// Dapper removes the prefix of the parameter names it creates. Drivers then have to find each parameter in the
    /// statement themselves: Microsoft.Data.Sqlite for instance looks for an unprefixed name with every supported prefix
    /// for each parameter, which makes a query with many parameters (e.g. an <c>IN</c> list) more than twice as slow.
    /// The parameters are still created by Dapper, so list expansion, type handlers and sizes are unchanged.
    /// </remarks>
    internal sealed class PrefixedParameters : SqlMapper.IDynamicParameters
    {
        private readonly object _template;
        private readonly Dictionary<string, object> _parameters;

        private PrefixedParameters(object template, Dictionary<string, object> parameters)
        {
            _template = template;
            _parameters = parameters;
        }

        /// <summary>
        /// Creates the parameters from the properties of an object, all referenced with '@' in the SQL text.
        /// </summary>
        public static PrefixedParameters FromTemplate(object template) => new(template, null);

        /// <summary>
        /// Creates the parameters from a dictionary. Only the names defined with an '@' get it back.
        /// </summary>
        public static PrefixedParameters FromDictionary(Dictionary<string, object> parameters) => new(null, parameters);

        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            ((SqlMapper.IDynamicParameters)new DynamicParameters(_template ?? _parameters)).AddParameters(command, identity);

            var sql = command.CommandText;

            foreach (IDataParameter parameter in command.Parameters)
            {
                var name = parameter.ParameterName;

                if (string.IsNullOrEmpty(name) || name[0] is '@' or ':' or '?' or '$')
                {
                    continue;
                }

                var prefixed = "@" + name;

                // Parameters expanded from a list by Dapper (e.g. "@Ids" to "@Ids1", "@Ids2", ...) keep the prefix of the
                // original name in the SQL text, which is always '@' for templates. Parameters from a dictionary, which
                // can be custom ones, only get it back if the SQL text uses it.
                if (_parameters == null || (_parameters.ContainsKey(prefixed) && sql.Contains(prefixed, StringComparison.Ordinal)))
                {
                    parameter.ParameterName = prefixed;
                }
            }
        }
    }
}
