
using YesSql.Sql;

namespace YesSql.Providers.SqlServer
{
    public class SqlServerCommandInterpreter : BaseCommandInterpreter
    {
        public SqlServerCommandInterpreter(ISqlDialect dialect) : base(dialect)
        {
        }
    }
}
