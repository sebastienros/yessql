using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using YesSql.Indexes;
using YesSql.Sql;

namespace YesSql.Commands
{
    public abstract class DocumentCommand : IIndexCommand
    {
        protected static readonly PropertyInfo[] AllProperties = new PropertyInfo[]
        {
            typeof(Document).GetProperty("Type")
        };

        protected static readonly PropertyInfo[] AllKeys = new PropertyInfo[]
        {
            typeof(Document).GetProperty("Id")
        };

        public abstract int ExecutionOrder { get; }

        public DocumentCommand(Document document)
        {
            Document = document;
        }

        public Document Document { get; }

        public abstract Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction, ISqlDialect dialect);
    }
}
