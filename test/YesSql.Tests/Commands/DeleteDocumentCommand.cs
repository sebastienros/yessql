using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Commands;

namespace YesSql.Tests.Commands
{
    public sealed class FailingCommand : DocumentCommand
    {
        public FailingCommand(Document document) : base(document, null)
        {

        }

        public override int ExecutionOrder => 4;

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand command, List<Action<DbDataReader>> actions)
        {
            throw new NotImplementedException();
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            throw new ApplicationException();
        }
    }
}
