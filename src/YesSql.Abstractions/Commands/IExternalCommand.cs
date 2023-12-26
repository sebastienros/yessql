using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public interface IExternalCommand : IIndexCommand
    {
        Task SetBatchCommand(string customBatchSql, IEnumerable<DbParameter> batchCommandParameters = null);
        Task SetCommand(string customSql, object param = null);
    }
}
