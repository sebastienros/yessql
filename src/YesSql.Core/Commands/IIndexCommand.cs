using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Logging;

namespace YesSql.Commands
{
    public interface IIndexCommand
    {
        Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger);
        int ExecutionOrder { get; }
    }
}