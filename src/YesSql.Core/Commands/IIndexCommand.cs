using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public interface IIndexCommand
    {
        Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger);
        int ExecutionOrder { get; }
    }
}