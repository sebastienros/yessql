using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public interface IExternalCommand : IIndexCommand
    {
        IExternalCommand SetBatchCommand(string customBatchSql, IEnumerable<DbParameter> batchCommandParameters = null);
        IExternalCommand SetCommand(string customSql, object param = null);
    }
}
