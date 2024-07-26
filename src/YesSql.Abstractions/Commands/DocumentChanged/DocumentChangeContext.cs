using System.Data.Common;

namespace YesSql.Commands.DocumentChanged
{
    public class DocumentChangeContext
    {
        public ISession Session { get; set; }
        public object Entity { get; set; }
        public Document Document { get; set; }
        public IStore Store { get; set; }
        public DbConnection Connection { get; set; }
        public DbTransaction Transaction { get; set; }
        public ISqlDialect Dialect { get; set; }
    }
}
