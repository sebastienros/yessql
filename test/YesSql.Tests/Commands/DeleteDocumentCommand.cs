using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Commands;

namespace YesSql.Tests.Commands
{
    public sealed class FailingCommand : DocumentCommand
    {
        public FailingCommand(Document document) : base(null, document)
        {

        }

        public override int ExecutionOrder => 4;

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            throw new ApplicationException();
        }
    }
}
