using System.Collections.Generic;
using System.Text;
using YesSql.Sql.Schema;

namespace YesSql.Sql
{
    public static class SchemaBuilderExtensions
    {
        public static IEnumerable<string> CreateSql(this ICommandInterpreter builder, ISchemaCommand command)
        {
            return builder.CreateSql(new[] { command });
        }
    }

}
