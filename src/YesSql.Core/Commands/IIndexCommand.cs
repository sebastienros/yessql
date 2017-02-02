using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Core.Sql;

namespace YesSql.Core.Commands
{
    public interface IIndexCommand
    {
        Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect);
        int ExecutionOrder { get; }
    }
}