using System.Collections.Generic;
using System.Text;
using YesSql.Sql.Schema;

namespace YesSql
{
    /// <summary>
    /// A class implementing this interface can execute database commands.
    /// </summary>
    public interface ICommandInterpreter
    {
        /// <summary>
        /// Generates the SQL statements for a set of schema commands.
        /// </summary>
        /// <param name="commands">The schema commands to translate.</param>
        /// <returns>The SQL statements that implement the commands.</returns>
        IEnumerable<string> CreateSql(IEnumerable<ISchemaCommand> commands);

        /// <summary>
        /// Generates the SQL statements for a create table command.
        /// </summary>
        /// <param name="command">The command to translate.</param>
        /// <returns>The SQL statements that create the table.</returns>
        IEnumerable<string> Run(ICreateTableCommand command);

        /// <summary>
        /// Generates the SQL statements for a drop table command.
        /// </summary>
        /// <param name="command">The command to translate.</param>
        /// <returns>The SQL statements that drop the table.</returns>
        IEnumerable<string> Run(IDropTableCommand command);

        /// <summary>
        /// Generates the SQL statements for an alter table command.
        /// </summary>
        /// <param name="command">The command to translate.</param>
        /// <returns>The SQL statements that alter the table.</returns>
        IEnumerable<string> Run(IAlterTableCommand command);

        /// <summary>
        /// Appends the SQL for an add column command to the provided builder.
        /// </summary>
        /// <param name="builder">The builder to append the SQL to.</param>
        /// <param name="command">The command to translate.</param>
        void Run(StringBuilder builder, IAddColumnCommand command);

        /// <summary>
        /// Appends the SQL for a drop column command to the provided builder.
        /// </summary>
        /// <param name="builder">The builder to append the SQL to.</param>
        /// <param name="command">The command to translate.</param>
        void Run(StringBuilder builder, IDropColumnCommand command);

        /// <summary>
        /// Appends the SQL for an alter column command to the provided builder.
        /// </summary>
        /// <param name="builder">The builder to append the SQL to.</param>
        /// <param name="command">The command to translate.</param>
        void Run(StringBuilder builder, IAlterColumnCommand command);

        /// <summary>
        /// Appends the SQL for an add index command to the provided builder.
        /// </summary>
        /// <param name="builder">The builder to append the SQL to.</param>
        /// <param name="command">The command to translate.</param>
        void Run(StringBuilder builder, IAddIndexCommand command);

        /// <summary>
        /// Appends the SQL for a drop index command to the provided builder.
        /// </summary>
        /// <param name="builder">The builder to append the SQL to.</param>
        /// <param name="command">The command to translate.</param>
        void Run(StringBuilder builder, IDropIndexCommand command);

        /// <summary>
        /// Generates the SQL statements for a raw SQL statement command.
        /// </summary>
        /// <param name="command">The command to translate.</param>
        /// <returns>The SQL statements to execute.</returns>
        IEnumerable<string> Run(ISqlStatementCommand command);

        /// <summary>
        /// Generates the SQL statements for a create foreign key command.
        /// </summary>
        /// <param name="command">The command to translate.</param>
        /// <returns>The SQL statements that create the foreign key.</returns>
        IEnumerable<string> Run(ICreateForeignKeyCommand command);

        /// <summary>
        /// Generates the SQL statements for a drop foreign key command.
        /// </summary>
        /// <param name="command">The command to translate.</param>
        /// <returns>The SQL statements that drop the foreign key.</returns>
        IEnumerable<string> Run(IDropForeignKeyCommand command);
    }
}
