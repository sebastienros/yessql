using System.Data;

namespace YesSql.Sql.Schema
{
    public interface ICreateColumnCommand : IColumnCommand
    {
        bool IsUnique { get; }

        bool IsNotNull { get; }

        bool IsPrimaryKey { get; }

        bool IsIdentity { get; }

        ICreateColumnCommand PrimaryKey();

        ICreateColumnCommand Identity();

        ICreateColumnCommand WithPrecision(byte precision);

        ICreateColumnCommand WithScale(byte scale);

        ICreateColumnCommand NotNull();

        ICreateColumnCommand Nullable();

        ICreateColumnCommand Unique();

        ICreateColumnCommand NotUnique();
    }
}
