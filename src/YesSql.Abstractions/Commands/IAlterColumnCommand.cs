using System;

namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a command that changes the definition of an existing column.
    /// </summary>
    public interface IAlterColumnCommand : IColumnCommand
    {
        /// <summary>
        /// Sets the data type of the column.
        /// </summary>
        /// <param name="dbType">The .NET type mapped to the database column type.</param>
        /// <param name="length">The maximum length of the column, or <c>null</c> for the default length.</param>
        /// <returns>The current <see cref="IAlterColumnCommand"/> instance so that calls can be chained.</returns>
        IAlterColumnCommand WithType(Type dbType, int? length);

        /// <summary>
        /// Sets the data type of the column using a fixed precision and scale.
        /// </summary>
        /// <param name="dbType">The .NET type mapped to the database column type.</param>
        /// <param name="precision">The total number of digits stored for the column.</param>
        /// <param name="scale">The number of digits stored to the right of the decimal point.</param>
        /// <returns>The current <see cref="IAlterColumnCommand"/> instance so that calls can be chained.</returns>
        IAlterColumnCommand WithType(Type dbType, byte precision, byte scale);
    }
}
