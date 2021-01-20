using System;
using System.Collections.Generic;
using System.Data;

namespace YesSql.Sql.Schema
{
    public static class SchemaUtils
    {
        private static Dictionary<Type, DbType> DbTypes = new Dictionary<Type, DbType>
        {
            { typeof(object), DbType.Binary },
            { typeof(byte[]), DbType.Binary },
            { typeof(string), DbType.String },
            { typeof(char), DbType.StringFixedLength },
            { typeof(bool), DbType.Boolean },
            { typeof(sbyte), DbType.SByte }, // not supported
            { typeof(sbyte), DbType.Int16 },
            { typeof(short), DbType.Int16 },
            { typeof(ushort), DbType.UInt16 }, // not supported
            { typeof(int), DbType.Int32 },
            { typeof(uint), DbType.UInt32 },
            { typeof(long), DbType.Int64 },
            { typeof(ulong), DbType.UInt64 },
            { typeof(float), DbType.Single },
            { typeof(double), DbType.Double },
            { typeof(decimal), DbType.Decimal },
            { typeof(DateTime), DbType.DateTime },
            { typeof(DateTimeOffset), DbType.DateTimeOffset },
            { typeof(Guid), DbType.Guid },
            { typeof(TimeSpan), DbType.Time },

            // Nullable types to prevent extra reflection on common ones
            { typeof(char?), DbType.StringFixedLength },
            { typeof(bool?), DbType.Boolean },
            { typeof(sbyte?), DbType.SByte },
            { typeof(sbyte?), DbType.Int16 },
            { typeof(short?), DbType.Int16 },
            { typeof(ushort?), DbType.UInt16 },
            { typeof(int?), DbType.Int32 },
            { typeof(uint?), DbType.UInt32 },
            { typeof(long?), DbType.Int64 },
            { typeof(ulong?), DbType.UInt64 },
            { typeof(float?), DbType.Single },
            { typeof(double?), DbType.Double },
            { typeof(decimal?), DbType.Decimal },
            { typeof(DateTime?), DbType.DateTime },
            { typeof(DateTimeOffset?), DbType.DateTimeOffset },
            { typeof(Guid?), DbType.Guid },
            { typeof(TimeSpan?), DbType.Time },

        };


        public static DbType ToDbType(Type type)
        {
            DbType dbType;

            if (DbTypes.TryGetValue(type, out dbType))
            {
                return dbType;
            }

            if (type.IsEnum)
            {
                return DbType.Int32;
            }

            // Nullable<T> ?
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var nullable = Nullable.GetUnderlyingType(type);

                if (nullable != null)
                {
                    return ToDbType(nullable);
                }
            }

            return DbType.Object;
        }
    }
}
