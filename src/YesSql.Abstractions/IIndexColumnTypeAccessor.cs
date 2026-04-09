using System;

namespace YesSql
{
    public interface IIndexColumnTypeAccessor
    {
        void SetIndexColumnType(Type indexType, string collection, string columnName, Type dbType);
        bool TryGetIndexColumnType(Type indexType, string collection, string columnName, out Type dbType);
        void RemoveIndexColumnTypes(Type indexType, string collection);
    }
}
