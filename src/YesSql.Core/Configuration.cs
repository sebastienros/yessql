using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using YesSql.Data;
using YesSql.Serialization;
using YesSql.Services;

namespace YesSql
{
    public class Configuration : IConfiguration
    {
        public Configuration()
        {
            IdentifierAccessorFactory = new PropertyAccessorFactory("Id");
            VersionAccessorFactory = new PropertyAccessorFactory("Version");
            ContentSerializer = new DefaultContentSerializer();
            IdGenerator = new DefaultIdGenerator();
            IsolationLevel = IsolationLevel.ReadCommitted;
            TablePrefix = "";
            CommandsPageSize = 500;
            QueryGatingEnabled = true;
            EnableThreadSafetyChecks = false;
            Logger = NullLogger.Instance;
            ConcurrentTypes = new HashSet<Type>();
            TableNameConvention = new DefaultTableNameConvention();
        }

        public IAccessorFactory IdentifierAccessorFactory { get; set; }
        public IAccessorFactory VersionAccessorFactory { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
        public IConnectionFactory ConnectionFactory { get; set; }
        public IContentSerializer ContentSerializer { get; set; }
        public string TablePrefix { get; set; }
        public string Schema { get; set; }
        public int CommandsPageSize { get; set; }
        public bool QueryGatingEnabled { get; set; }
        public bool EnableThreadSafetyChecks { get; set; }
        public IIdGenerator IdGenerator { get; set; }
        public ILogger Logger { get; set; }
        public HashSet<Type> ConcurrentTypes { get; }
        public ITableNameConvention TableNameConvention { get; set; }
        public ICommandInterpreter CommandInterpreter { get; set; }
        public ISqlDialect SqlDialect { get; set; }
        public IdentityColumnSize IdentityColumnSize { get; set; } = IdentityColumnSize.Int32;
    }
}
