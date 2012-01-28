using System.Collections.Generic;
using System.Linq;
using YesSql.Core.Data.Models;

namespace YesSql.Core.Indexes
{
    public interface IHasDocumentsIndex : IIndex
    {
    }

    public abstract class HasDocumentsIndex : IHasDocumentsIndex
    {
        protected HasDocumentsIndex()
        {
            Documents = new HashSet<Document>();
        }

        public virtual int Id { get; set; }
        public virtual ICollection<Document> Documents { get; set; }
    }
}
