using System;

namespace YesSql.Services
{
    public class DefaultTableNameConvention : ITableNameConvention
    {
        public const string DocumentTable = "Document";
        
        public string GetIndexTable(Type type, string collection = null)
        {
            if (string.IsNullOrEmpty(collection))
            {
                return type.Name;
            }

            return collection + "_" + type.Name;
        }

        public string GetDocumentTable(string collection = null)
        {
            if (string.IsNullOrEmpty(collection))
            {
                return DocumentTable;
            }

            return collection + "_" + DocumentTable;

        }
    }
}
