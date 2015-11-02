using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using YesSql.Core.Sql.Schema;

namespace YesSql.Core.Sql.SchemaBuilders
{
    public abstract class BaseSchemaBuilder : ISchemaBuilder
    {
        protected readonly ISqlDialect _dialect;
        private const char Space = ' ';

        public BaseSchemaBuilder(ISqlDialect dialect)
        {
            _dialect = dialect;
        }

        public IEnumerable<string> CreateSql(IEnumerable<ISchemaBuilderCommand> commands)
        {

            var sqlCommands = new List<string>();

            foreach (var command in commands)
            {
                var schemaCommand = command as SchemaCommand;
                if (schemaCommand == null)
                {
                    continue;
                }

                switch (schemaCommand.Type)
                {
                    case SchemaCommandType.CreateTable:
                        sqlCommands.AddRange(Run((CreateTableCommand)schemaCommand));
                        break;
                    case SchemaCommandType.AlterTable:
                        sqlCommands.AddRange(Run((AlterTableCommand)schemaCommand));
                        break;
                    case SchemaCommandType.DropTable:
                        sqlCommands.AddRange(Run((DropTableCommand)schemaCommand));
                        break;
                    case SchemaCommandType.SqlStatement:
                        sqlCommands.AddRange(Run((SqlStatementCommand)schemaCommand));
                        break;
                    case SchemaCommandType.CreateForeignKey:
                        sqlCommands.AddRange(Run((CreateForeignKeyCommand)schemaCommand));
                        break;
                    case SchemaCommandType.DropForeignKey:
                        sqlCommands.AddRange(Run((DropForeignKeyCommand)schemaCommand));
                        break;
                }
            }

            return sqlCommands;
        }

        public virtual IEnumerable<string> Run(CreateTableCommand command)
        {
            // TODO: Support CreateForeignKeyCommand in a CREATE TABLE (in sqlite they can only be created with the table)

            var builder = new StringBuilder();

            builder.Append(_dialect.CreateTableString)
                .Append(' ')
                .Append(_dialect.QuoteForTableName(command.Name))
                .Append(" (");

            var appendComma = false;
            foreach (var createColumn in command.TableCommands.OfType<CreateColumnCommand>())
            {
                if (appendComma)
                {
                    builder.Append(", ");
                }
                appendComma = true;

                Run(builder, createColumn);
            }

            var primaryKeys = command.TableCommands.OfType<CreateColumnCommand>().Where(ccc => ccc.IsPrimaryKey && !ccc.IsIdentity).Select(ccc => ccc.ColumnName).ToArray();
            if (primaryKeys.Any())
            {
                if (appendComma)
                {
                    builder.Append(", ");
                }

                builder.Append(_dialect.PrimaryKeyString)
                    .Append(" ( ")
                    .Append(String.Join(", ", primaryKeys.ToArray()))
                    .Append(" )");
            }

            builder.Append(" )");
            yield return builder.ToString();
        }

        public virtual IEnumerable<string> Run(DropTableCommand command)
        {
            var builder = new StringBuilder();

            builder.Append(_dialect.GetDropTableString(command.Name));
            yield return builder.ToString();
        }

        public virtual IEnumerable<string> Run(AlterTableCommand command)
        {
            if (command.TableCommands.Count == 0)
            {
                yield break;
            }

            var commands = new List<string>();

            // drop columns
            foreach (var dropColumn in command.TableCommands.OfType<DropColumnCommand>())
            {
                var builder = new StringBuilder();
                Run(builder, dropColumn);
            }

            // add columns
            foreach (var addColumn in command.TableCommands.OfType<AddColumnCommand>())
            {
                var builder = new StringBuilder();
                Run(builder, addColumn);
                yield return builder.ToString();
            }

            // alter columns
            foreach (var alterColumn in command.TableCommands.OfType<AlterColumnCommand>())
            {
                var builder = new StringBuilder();
                Run(builder, alterColumn);
                yield return builder.ToString();
            }

            // add index
            foreach (var addIndex in command.TableCommands.OfType<AddIndexCommand>())
            {
                var builder = new StringBuilder();
                Run(builder, addIndex);
                yield return builder.ToString();
            }

            // drop index
            foreach (var dropIndex in command.TableCommands.OfType<DropIndexCommand>())
            {
                var builder = new StringBuilder();
                Run(builder, dropIndex);
                yield return builder.ToString();
            }
        }

        public virtual void Run(StringBuilder builder, AddColumnCommand command)
        {
            builder.AppendFormat("alter table {0} add ", _dialect.QuoteForTableName(command.TableName));
            Run(builder, (CreateColumnCommand)command);
        }

        public virtual void Run(StringBuilder builder, DropColumnCommand command)
        {
            builder.AppendFormat("alter table {0} drop column {1}",
                _dialect.QuoteForTableName(command.TableName),
                _dialect.QuoteForColumnName(command.ColumnName));
        }

        public virtual void Run(StringBuilder builder, AlterColumnCommand command)
        {
            builder.AppendFormat("alter table {0} alter column {1} ",
                _dialect.QuoteForTableName(command.TableName),
                _dialect.QuoteForColumnName(command.ColumnName));

            // type
            if (command.DbType != DbType.Object)
            {
                builder.Append(_dialect.GetTypeName(command.DbType, command.Length, command.Precision, command.Scale));
            }
            else
            {
                if (command.Length > 0 || command.Precision > 0 || command.Scale > 0)
                {
                    throw new Exception("Error while executing data migration: you need to specify the field's type in order to change its properties");
                }
            }

            // [default value]
            if (command.Default != null)
            {
                builder.Append(" set default ").Append(_dialect.GetSqlValue(command.Default)).Append(Space);
            }
        }

        public virtual void Run(StringBuilder builder, AddIndexCommand command)
        {
            builder.AppendFormat("create index {1} on {0} ({2}) ",
                _dialect.QuoteForTableName(command.TableName),
                _dialect.QuoteForColumnName(command.IndexName),
                String.Join(", ", command.ColumnNames));
        }

        public virtual void Run(StringBuilder builder, DropIndexCommand command)
        {
            builder.AppendFormat("drop index {0} ON {1}",
                _dialect.QuoteForColumnName(command.IndexName),
                _dialect.QuoteForTableName(command.TableName));
        }

        public virtual IEnumerable<string> Run(SqlStatementCommand command)
        {
            if (command.Providers.Count != 0)
            {
                yield break;
            }

            yield return command.Sql;
        }

        public virtual IEnumerable<string> Run(CreateForeignKeyCommand command)
        {
            var builder = new StringBuilder();

            builder.Append("alter table ")
                .Append(_dialect.QuoteForTableName(command.SrcTable));

            builder.Append(_dialect.GetAddForeignKeyConstraintString(command.Name,
                command.SrcColumns,
                _dialect.QuoteForTableName(command.DestTable),
                command.DestColumns,
                false));

            yield return builder.ToString();
        }

        public virtual IEnumerable<string> Run(DropForeignKeyCommand command)
        {
            var builder = new StringBuilder();

            builder.Append("alter table ")
                .Append(_dialect.QuoteForTableName(command.SrcTable))
                .Append(_dialect.GetDropForeignKeyConstraintString(command.Name));
            yield return builder.ToString();
        }

        private void Run(StringBuilder builder, CreateColumnCommand command)
        {
            // name
            builder.Append(_dialect.QuoteForColumnName(command.ColumnName)).Append(Space);

            if (!command.IsIdentity || _dialect.HasDataTypeInIdentityColumn)
            {
                builder.Append(_dialect.GetTypeName(command.DbType, command.Length, command.Precision, command.Scale));
            }

            // append identity if handled
            if (command.IsIdentity && _dialect.SupportsIdentityColumns)
            {
                builder.Append(Space).Append(_dialect.IdentityColumnString);
            }

            // [default value]
            if (command.Default != null)
            {
                builder.Append(" default ").Append(_dialect.GetSqlValue(command.Default)).Append(Space);
            }

            // nullable
            builder.Append(command.IsNotNull
                               ? " not null"
                               : !command.IsPrimaryKey && !command.IsUnique
                                     ? _dialect.NullColumnString
                                     : string.Empty);

            // append unique if handled, otherwise at the end of the satement
            if (command.IsUnique && _dialect.SupportsUnique)
            {
                builder.Append(" unique");
            }
        }
    }
}
