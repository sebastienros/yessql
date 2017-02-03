using System.Collections.Generic;
using YesSql.Core.Sql.Schema;

namespace YesSql.Core.Sql.Providers.Sqlite
{
    public class SqliteComandInterpreter : BaseComandInterpreter
    {
        public SqliteComandInterpreter(ISqlDialect dialect) : base(dialect)
        {
        }

        public override IEnumerable<string> Run(CreateForeignKeyCommand command)
        {
            yield break;
        }

        public override IEnumerable<string> Run(DropForeignKeyCommand command)
        {
            yield break;
        }
    }
}
