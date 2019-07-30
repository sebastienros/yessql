using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

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

        public abstract Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger);
    }
}
