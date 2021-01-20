using System;

namespace YesSql.Sql.Schema
{
    public interface IAlterColumnCommand : IColumnCommand
    {
        IAlterColumnCommand WithType(Type dbType, int? length);
        IAlterColumnCommand WithType(Type dbType, byte precision, byte scale);
    }
}
