using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public class ExternalCommand : IIndexCommand
    {
        public int ExecutionOrder { get; } = 0;
        private readonly string _customSqlCmd;
        private readonly object _param = null;
        public readonly bool _batchable;
        private readonly IEnumerable<DbParameter> _batchCommandParameters;
        public ExternalCommand(string customSqlCmd, object param = null, bool batchable = false, IEnumerable<DbParameter> batchCommandParameters = null)
        {
            _customSqlCmd = customSqlCmd;
            _param = param;
            _batchable = batchable;
            _batchCommandParameters = batchCommandParameters;
        }

        public bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand batchCommand, List<Action<DbDataReader>> actions, int index)
        {
            if (_batchable)
            {
                queries.Add(_customSqlCmd);
                if (_batchCommandParameters != null)
                {
                    foreach (var parameter in _batchCommandParameters)
                    {
                        batchCommand.AddParameter(parameter.ParameterName, parameter.Value, parameter.DbType);
                    }
                }
                return true;
            }
            
            return false;
        }

        public Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(_customSqlCmd);
            }

            return connection.ExecuteAsync(_customSqlCmd, _param, transaction);
        }
    }
}
