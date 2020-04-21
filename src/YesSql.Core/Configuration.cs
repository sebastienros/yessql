using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using YesSql.Data;
using YesSql.Naming;
using YesSql.Serialization;
using YesSql.Services;

namespace YesSql
{
    public class Configuration : IConfiguration
    {
        public Configuration()
        {
            IdentifierFactory = new DefaultIdentifierFactory();
            ContentSerializer = new JsonContentSerializer();
            IdGenerator = new DefaultIdGenerator();
            IsolationLevel = IsolationLevel.ReadCommitted;
            TablePrefix = "";
            SessionPoolSize = 16;
            QueryGatingEnabled = true;
            Logger = NullLogger.Instance;
            ConcurrentTypes = new HashSet<Type>();
            NamingCase = NamingCase.PascalCase;
        }

        public IIdentifierFactory IdentifierFactory { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
        public IConnectionFactory ConnectionFactory { get; set; }
        public IContentSerializer ContentSerializer { get; set; }
        private string _tablePrefix;
        private NamingCase _namingCase;

        public string TablePrefix
        {
            get =>
                // Snake case requires a _ after the prefix, add it if it does not yet exists
                NamingCase == NamingCase.SnakeCase && !_tablePrefix.EndsWith("_")
                    ? _tablePrefix + "_"
                    : _tablePrefix;
            set => _tablePrefix = value;
        }

        public int SessionPoolSize { get; set; }
        public bool QueryGatingEnabled { get; set; }
        public IIdGenerator IdGenerator { get; set; }
        public ILogger Logger { get; set; }
        public HashSet<Type> ConcurrentTypes { get; }

        public NamingCase NamingCase
        {
            get => _namingCase;
            set
            {
                _namingCase = value;
                if (_namingCase == NamingCase.SnakeCase)
                {
                    // By default, Dapper expects the columns returned by a query to match the property names of the type you're mapping to.
                    // For example, that means Dapper expects a column named DisplayName, but the actual column name is display_name.
                    Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
                }
            }
        }
    }
}
