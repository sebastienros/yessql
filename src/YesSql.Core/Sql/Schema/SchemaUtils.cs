using System;
using System.Data;
using YesSql.Core.Serialization;

namespace YesSql.Core.Sql.Schema {
    public static class SchemaUtils {
        public static DbType ToDbType(Type type) {
            DbType dbType;
            switch ( type.GetTypeCode() ) {
                case TypeCode.String:
                    dbType = DbType.String;
                    break;
                case TypeCode.Int32:
                    dbType = DbType.Int32;
                    break;
                case TypeCode.DateTime:
                    dbType = DbType.DateTime;
                    break;
                case TypeCode.Boolean:
                    dbType = DbType.Boolean;
                    break;
                default:
                    if(type == typeof(Guid)) 
                        dbType = DbType.Guid;
                    else
                        Enum.TryParse(type.GetTypeCode().ToString(), true, out dbType);
                    break;
            }

            return dbType;
        }

    }
}
