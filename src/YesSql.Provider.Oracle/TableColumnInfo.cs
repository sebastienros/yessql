using Dapper.Oracle;

namespace YesSql.Provider.Oracle
{
    public class TableColumnInfo
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }

        public OracleMappingType OracleMappingType 
        {
            get
            {
                switch (DataType)
                {
                    case "NUMBER":
                        return OracleMappingType.Int32;
                    case "NCLOB":
                        return OracleMappingType.NClob;
                    case "BLOB":
                        return OracleMappingType.Blob;
                    case "TIMESTAMP(6)":
                        return OracleMappingType.TimeStamp;
                    case "BINARY_FLOAT":
                        return OracleMappingType.BinaryFloat;
                    case "BINARY_DOUBLE":
                        return OracleMappingType.BinaryDouble;
                    case "NVARCHAR2":
                        return OracleMappingType.NVarchar2;
                    case "INTEGER":
                        return OracleMappingType.Int32;
                    default:
                        return OracleMappingType.NVarchar2;
                }
            }
        }
    }
}