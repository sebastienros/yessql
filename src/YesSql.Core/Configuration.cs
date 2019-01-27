using System.Data;
using YesSql.Data;
using YesSql.Serialization;

namespace YesSql
{
    public class Configuration : IConfiguration
    {
        public Configuration()
        {
            IdentifierFactory = new DefaultIdentifierFactory();
            ContentSerializer = new JsonContentSerializer();
            IsolationLevel = IsolationLevel.ReadCommitted;
            TablePrefix = "";
            SessionPoolSize = 16;
            IdBlockSize = 20;
            QueryGatingEnabled = true;
        }

        public IIdentifierFactory IdentifierFactory { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
        public IConnectionFactory ConnectionFactory { get; set; }
        public IContentSerializer ContentSerializer { get; set; }
        public string TablePrefix { get; set; }
        public int SessionPoolSize { get; set; }
        public bool QueryGatingEnabled { get; set; }
        public int IdBlockSize { get; set; }
    }
}
