using YesSql.Sql;

namespace YesSql.Provider.SqlServer
{
    public class SqlServerCommandInterpreter : BaseCommandInterpreter
    {
        public SqlServerCommandInterpreter(ISqlDialect dialect) : base(dialect)
        {
        }
    }
}
