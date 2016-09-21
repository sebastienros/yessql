using System;
using System.Collections.Generic;
using System.Data;

namespace YesSql.Core.Sql.Schema
{
    public static class SchemaUtils
    {
        private static Dictionary<Type, DbType> DbTypes = new Dictionary<Type, DbType>
        {
                {typeof(object), DbType.Binary},
                {typeof(string), DbType.String},
                {typeof(char), DbType.String},
                {typeof(bool), DbType.Boolean},
                {typeof(sbyte), DbType.SByte},
                {typeof(short), DbType.Int16},
                {typeof(ushort), DbType.UInt16},
                {typeof(int), DbType.Int32},
                {typeof(uint), DbType.UInt32},
                {typeof(long), DbType.Int64},
                {typeof(ulong), DbType.UInt64},
                {typeof(float), DbType.Single},
                {typeof(double), DbType.Double},
                {typeof(decimal), DbType.Decimal},
                {typeof(DateTime), DbType.DateTime2},
                {typeof(DateTimeOffset), DbType.DateTimeOffset},
                {typeof(Guid), DbType.DateTime}
        };


        public static DbType ToDbType(Type type)
        {
            DbType dbType;

            if(DbTypes.TryGetValue(type, out dbType))
            {
                return dbType;
            }

            return DbType.Object;
        }
    }
}
