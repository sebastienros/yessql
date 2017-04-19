using System.Data;

namespace YesSql.Sql.Schema
{
    public class CreateColumnCommand : ColumnCommand, ICreateColumnCommand
    {
        public CreateColumnCommand(string tableName, string name) : base(tableName, name)
        {
            IsNotNull = false;
            IsUnique = false;
        }

        public bool IsUnique { get; protected set; }

        public bool IsNotNull { get; protected set; }

        public bool IsPrimaryKey { get; protected set; }

        public bool IsIdentity { get; protected set; }

        public ICreateColumnCommand PrimaryKey()
        {
            IsPrimaryKey = true;
            IsUnique = false;
            return this;
        }

        public ICreateColumnCommand Identity()
        {
            IsIdentity = true;
            IsUnique = false;
            return this;
        }

        public ICreateColumnCommand WithPrecision(byte precision)
        {
            Precision = precision;
            return this;
        }

        public ICreateColumnCommand WithScale(byte scale)
        {
            Scale = scale;
            return this;
        }

        public ICreateColumnCommand NotNull()
        {
            IsNotNull = true;
            return this;
        }

        public ICreateColumnCommand Nullable()
        {
            IsNotNull = false;
            return this;
        }

        public ICreateColumnCommand Unique()
        {
            IsUnique = true;
            IsPrimaryKey = false;
            IsIdentity = false;
            return this;
        }

        public ICreateColumnCommand NotUnique()
        {
            IsUnique = false;
            return this;
        }

        public new ICreateColumnCommand WithLength(int? length)
        {
            base.WithLength(length);
            return this;
        }

        public new ICreateColumnCommand Unlimited()
        {
            return WithLength(10000);
        }

        public new ICreateColumnCommand WithType(DbType dbType)
        {
            base.WithType(dbType);
            return this;
        }

        public new ICreateColumnCommand WithDefault(object @default)
        {
            base.WithDefault(@default);
            return this;
        }
    }
}
