using System.Data;

namespace YesSql.Sql.Schema
{
    public interface IAlterColumnCommand : IColumnCommand
    {
        IAlterColumnCommand WithType(DbType dbType, int? length);
        IAlterColumnCommand WithType(DbType dbType, byte precision, byte scale);
    }
}
