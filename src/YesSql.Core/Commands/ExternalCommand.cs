using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public class ExternalCommand : IExternalCommand
    {
        public int ExecutionOrder { get; set; } = 0;
        private string _customSql;
        private string _customBatchSql;
        private object _param = null;
        public bool _batchable;
        private IEnumerable<DbParameter> _batchCommandParameters;

        public Task SetBatchCommand(string customBatchSql, IEnumerable<DbParameter> batchCommandParameters = null)
        {
            _customBatchSql = customBatchSql;
            _batchCommandParameters = batchCommandParameters;
            _batchable = true;
            return Task.CompletedTask;
        }
        public Task SetCommand(string customSql, object param = null)
        {
            _customSql = customSql;
            _param = param;
            return Task.CompletedTask;
        }
        public ExternalCommand(string customSql = null, object param = null, bool batchable = false, string customBatchSql = null, IEnumerable<DbParameter> batchCommandParameters = null)
        {
            _customSql = customSql;
            _param = param;
            _batchable = batchable;
            _customBatchSql = customBatchSql;
            _batchCommandParameters = batchCommandParameters;
        }

        public bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand batchCommand, List<Action<DbDataReader>> actions, int index)
        {
            if (_batchable && !string.IsNullOrEmpty(_customBatchSql))
            {
                queries.Add(_customBatchSql);
                if (_batchCommandParameters != null)
                {
                    batchCommand.Parameters.AddRange(_batchCommandParameters.ToArray());
                }
                return true;
            }
            return false;
        }

        public Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(_customSql);
            }

            return connection.ExecuteAsync(_customSql, _param, transaction);
        }
    }
}
