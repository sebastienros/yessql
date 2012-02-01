using System.Collections.Generic;
using YesSql.Core.Data.Models;

namespace YesSql.Core.Indexes
{
    public abstract class ReduceIndex : IHasDocumentsIndex
    {
        protected ReduceIndex()
        {
            Documents = new HashSet<Document>();
        }

        public virtual int Id { get; set; }
        public virtual ICollection<Document> Documents { get; private set; }
    }
}