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
            IdentifierFactory = new DefaultIdentifierFactory();
            ContentSerializer = new JsonContentSerializer();
            IdGenerator = new DefaultIdGenerator();
            IsolationLevel = IsolationLevel.ReadCommitted;
            TablePrefix = "";
            SessionPoolSize = 16;
            QueryGatingEnabled = true;
        }

        public IIdentifierFactory IdentifierFactory { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
        public IConnectionFactory ConnectionFactory { get; set; }
        public IContentSerializer ContentSerializer { get; set; }
        public string TablePrefix { get; set; }
        public int SessionPoolSize { get; set; }
        public bool QueryGatingEnabled { get; set; }
        public IIdGenerator IdGenerator { get; set; }
    }
}
