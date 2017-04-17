using System;
using System.Data;
using System.Data.Common;
using YesSql.Data;
using YesSql.Storage;

namespace YesSql
{
    public class Configuration : IConfiguration
    {
        public Configuration()
        {
            IdentifierFactory = new DefaultIdentifierFactory();
            IsolationLevel = IsolationLevel.ReadCommitted;
            TablePrefix = "";
        }

        public IIdentifierFactory IdentifierFactory { get; set; }
        public IDocumentStorageFactory DocumentStorageFactory { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
        public IConnectionFactory ConnectionFactory { get; set; }
        public string TablePrefix { get; set; }

        public Configuration SetIdentifierFactory(IIdentifierFactory identifierFactory)
        {
            IdentifierFactory = identifierFactory;
            return this;
        }

        public Configuration SetDocumentStorageFactory(IDocumentStorageFactory documentStorageFactory)
        {
            DocumentStorageFactory = documentStorageFactory;
            return this;
        }

        public Configuration SetIsolationLevel(IsolationLevel isolationLevel)
        {
            IsolationLevel = isolationLevel;
            return this;
        }

        public Configuration SetConnectionFactory(IConnectionFactory connectionFactory)
        {
            ConnectionFactory = connectionFactory;
            return this;
        }

        public Configuration SetTablePrefix(string tablePrefix)
        {
            TablePrefix = tablePrefix;
            return this;
        }
    }
}
