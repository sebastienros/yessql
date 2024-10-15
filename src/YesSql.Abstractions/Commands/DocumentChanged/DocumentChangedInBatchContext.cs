using System.Collections.Generic;
using System.Data.Common;

namespace YesSql.Commands.DocumentChanged
{
    public class DocumentChangeInBatchContext
    {
        public object Entity { get; set; }
        public List<string> Queries { get; set; }
        public DbCommand BatchCommand { get; set; }
        public ISession Session { get; set; }
        public Document Document { get; set; }
    }
}
