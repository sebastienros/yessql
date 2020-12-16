using Dapper;
using System;
using System.Data;

namespace YesSql.Provider.Sqlite
{
    public class SqliteGuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value)
        {
            return new Guid((string)value);
        }

        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
        }
    }
}
