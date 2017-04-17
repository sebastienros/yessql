using System.Data;
using System.Threading.Tasks;
using YesSql.Sql;

namespace YesSql.Commands
{
    public interface IIndexCommand
    {
        Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction, ISqlDialect dialect);
        int ExecutionOrder { get; }
    }
}