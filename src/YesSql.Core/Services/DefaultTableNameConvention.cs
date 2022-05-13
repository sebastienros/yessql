using System;
using System.Reflection;

namespace YesSql.Services
{
    public class DefaultTableNameConvention : ITableNameConvention
    {
        public const string DocumentTable = "Document";

        public string GetIndexTable(Type type, string collection = null)
        {
            var tableName = type.Name;

            var attribute = type.GetCustomAttribute<TableNameAttribute>();

            if (attribute != null && !String.IsNullOrEmpty(attribute.Name))
            {
                tableName = attribute.Name;
            }

            if (String.IsNullOrEmpty(collection))
            {
                return tableName;
            }

            return collection + "_" + type.Name;
        }

        public string GetDocumentTable(string collection = null)
        {
            if (String.IsNullOrEmpty(collection))
            {
                return DocumentTable;
            }

            return collection + "_" + DocumentTable;
        }
    }
}
