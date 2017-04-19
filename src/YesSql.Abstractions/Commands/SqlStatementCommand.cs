using System.Collections.Generic;

namespace YesSql.Sql.Schema
{
    public interface ISqlStatementCommand : ISchemaCommand
    {
        string Sql { get; }
        List<string> Providers { get; }
    }
}
