using System.Collections.Generic;

namespace YesSql.Core.Sql.Schema {
    public class SqlStatementCommand : SchemaCommand {
        protected readonly List<string> _providers;
        public SqlStatementCommand(string sql)
            : base(string.Empty, SchemaCommandType.SqlStatement) {
            Sql = sql;
            _providers = new List<string>();
        }

        public string Sql { get; private set; }
        public List<string> Providers { get { return _providers; } }

        public SqlStatementCommand ForProvider(string dataProvider) {
            _providers.Add(dataProvider);
            return this;
        }
    }
}
