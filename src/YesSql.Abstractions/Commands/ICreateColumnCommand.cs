namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a command that creates a new column on a table.
    /// </summary>
    public interface ICreateColumnCommand : IColumnCommand
    {
        /// <summary>
        /// Gets a value indicating whether the column has a unique constraint.
        /// </summary>
        bool IsUnique { get; }

        /// <summary>
        /// Gets a value indicating whether the column does not accept <c>null</c> values.
        /// </summary>
        bool IsNotNull { get; }

        /// <summary>
        /// Gets a value indicating whether the column is part of the primary key.
        /// </summary>
        bool IsPrimaryKey { get; }

        /// <summary>
        /// Gets a value indicating whether the column is an auto-incremented identity column.
        /// </summary>
        bool IsIdentity { get; }

        /// <summary>
        /// Marks the column as the primary key of the table.
        /// </summary>
        /// <returns>The current <see cref="ICreateColumnCommand"/> instance so that calls can be chained.</returns>
        ICreateColumnCommand PrimaryKey();

        /// <summary>
        /// Marks the column as an auto-incremented identity column.
        /// </summary>
        /// <returns>The current <see cref="ICreateColumnCommand"/> instance so that calls can be chained.</returns>
        ICreateColumnCommand Identity();

        /// <summary>
        /// Sets the total number of digits stored for the column.
        /// </summary>
        /// <param name="precision">The precision, or <c>null</c> for the default precision.</param>
        /// <returns>The current <see cref="ICreateColumnCommand"/> instance so that calls can be chained.</returns>
        ICreateColumnCommand WithPrecision(byte? precision);

        /// <summary>
        /// Sets the number of digits stored to the right of the decimal point.
        /// </summary>
        /// <param name="scale">The scale, or <c>null</c> for the default scale.</param>
        /// <returns>The current <see cref="ICreateColumnCommand"/> instance so that calls can be chained.</returns>
        ICreateColumnCommand WithScale(byte? scale);

        /// <summary>
        /// Marks the column as not accepting <c>null</c> values.
        /// </summary>
        /// <returns>The current <see cref="ICreateColumnCommand"/> instance so that calls can be chained.</returns>
        ICreateColumnCommand NotNull();

        /// <summary>
        /// Marks the column as accepting <c>null</c> values.
        /// </summary>
        /// <returns>The current <see cref="ICreateColumnCommand"/> instance so that calls can be chained.</returns>
        ICreateColumnCommand Nullable();

        /// <summary>
        /// Adds a unique constraint to the column.
        /// </summary>
        /// <returns>The current <see cref="ICreateColumnCommand"/> instance so that calls can be chained.</returns>
        ICreateColumnCommand Unique();

        /// <summary>
        /// Removes the unique constraint from the column.
        /// </summary>
        /// <returns>The current <see cref="ICreateColumnCommand"/> instance so that calls can be chained.</returns>
        ICreateColumnCommand NotUnique();
    }
}
