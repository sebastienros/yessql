using System;

namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a command that operates on a single column of a table.
    /// </summary>
    public interface IColumnCommand : ITableCommand
    {
        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        string ColumnName { get; }

        /// <summary>
        /// Gets the number of digits stored to the right of the decimal point, or <c>null</c> when not specified.
        /// </summary>
        byte? Scale { get; }

        /// <summary>
        /// Gets the total number of digits stored for the column, or <c>null</c> when not specified.
        /// </summary>
        byte? Precision { get; }

        /// <summary>
        /// Gets the .NET type mapped to the database column type.
        /// </summary>
        Type DbType { get; }

        /// <summary>
        /// Gets the default value assigned to the column.
        /// </summary>
        object Default { get; }

        /// <summary>
        /// Gets the maximum length of the column, or <c>null</c> when not specified.
        /// </summary>
        int? Length { get; }

        /// <summary>
        /// Sets the default value of the column.
        /// </summary>
        /// <param name="default">The default value to assign to the column.</param>
        /// <returns>The current <see cref="IColumnCommand"/> instance so that calls can be chained.</returns>
        IColumnCommand WithDefault(object @default);

        /// <summary>
        /// Sets the maximum length of the column.
        /// </summary>
        /// <param name="length">The maximum length, or <c>null</c> for the default length.</param>
        /// <returns>The current <see cref="IColumnCommand"/> instance so that calls can be chained.</returns>
        IColumnCommand WithLength(int? length);

        /// <summary>
        /// Configures the column to store an unlimited amount of data.
        /// </summary>
        /// <returns>The current <see cref="IColumnCommand"/> instance so that calls can be chained.</returns>
        IColumnCommand Unlimited();
    }
}
