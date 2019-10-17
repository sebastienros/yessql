using System.Data;

namespace YesSql.Sql.Schema
{
    public class ColumnCommand : TableCommand, IColumnCommand
    {
        public string ColumnName { get; set; }

        public ColumnCommand(string tableName, string name)
            : base(tableName)
        {
            ColumnName = name;
            DbType = DbType.Object;
            Default = null;
            Length = null;
        }
        public byte Scale { get; protected set; }

        public byte Precision { get; protected set; }

        public DbType DbType { get; private set; }

        public object Default { get; private set; }

        public int? Length { get; private set; }

        public IColumnCommand WithType(DbType dbType)
        {
            DbType = dbType;
            return this;
        }

        public IColumnCommand WithDefault(object @default)
        {
            Default = @default;
            return this;
        }


        public IColumnCommand WithLength(int? length)
        {
            Length = length;
            return this;
        }

        public IColumnCommand Unlimited()
        {
            return WithLength(10000);
        }
    }
}
