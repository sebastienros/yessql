using System;
using System.Data;
using System.Threading.Tasks;

namespace YesSql.Core.Commands
{
    public interface IIndexCommand
    {
        Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction);
    }
}