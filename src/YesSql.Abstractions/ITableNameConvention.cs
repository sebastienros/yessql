using System;

namespace YesSql
{
    public interface ITableNameConvention
    {
        string GetDocumentTable(string collection = null);
        string GetIndexTable(Type indexType, string collection = null);
    }
}
