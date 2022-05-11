using System;

namespace YesSql.Services
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class TableNameAttribute : Attribute
    {
        public string Name { get; } = String.Empty;

        public TableNameAttribute(string tableName)
        {
            if (String.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            Name = tableName;
        }
    }
}
