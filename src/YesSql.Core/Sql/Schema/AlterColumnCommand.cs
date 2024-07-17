using System;

namespace YesSql.Sql.Schema
{
    public class AlterColumnCommand : ColumnCommand, IAlterColumnCommand
    {
        public AlterColumnCommand(string tableName, string columnName)
            : base(tableName, columnName)
        {
        }

        public new IAlterColumnCommand WithType(Type dbType)
        {
            base.WithType(dbType);
            return this;
        }

        public IAlterColumnCommand WithType(Type dbType, int? length)
        {
            base.WithType(dbType).WithLength(length);
            return this;
        }

        public IAlterColumnCommand WithType(Type dbType, byte precision, byte scale)
        {
            base.WithType(dbType);
            Precision = precision;
            Scale = scale;
            return this;
        }

        public new IAlterColumnCommand WithLength(int? length)
        {
            base.WithLength(length);
            return this;
        }

        public new IAlterColumnCommand Unlimited() => WithLength(16385);
    }
}
