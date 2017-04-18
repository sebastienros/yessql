using System.Collections.Generic;
using System.Text;
using YesSql.Sql.Schema;

namespace YesSql.Sql
{
    public interface ICommandInterpreter
    {
        IEnumerable<string> CreateSql(IEnumerable<ISchemaCommand> commands);
        IEnumerable<string> Run(CreateTableCommand command);
        IEnumerable<string> Run(DropTableCommand command);
        IEnumerable<string> Run(AlterTableCommand command);
        void Run(StringBuilder builder, AddColumnCommand command);
        void Run(StringBuilder builder, DropColumnCommand command);
        void Run(StringBuilder builder, AlterColumnCommand command);
        void Run(StringBuilder builder, AddIndexCommand command);
        void Run(StringBuilder builder, DropIndexCommand command);
        IEnumerable<string> Run(SqlStatementCommand command);
        IEnumerable<string> Run(CreateForeignKeyCommand command);
        IEnumerable<string> Run(DropForeignKeyCommand command);
    }

    public static class SchemaBuilderExtensions
    {
        public static IEnumerable<string> CreateSql(this ICommandInterpreter builder, ISchemaCommand command)
        {
            return builder.CreateSql(new[] { command });
        }
    }

}
