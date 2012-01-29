using System.Collections.Generic;
using YesSql.Core.Data.Models;

namespace YesSql.Core.Indexes
{
    public interface IHasDocumentsIndex : IIndex
    {
    }

    public abstract class HasDocumentsIndex : IHasDocumentsIndex, IIndexProvider
    {
        protected HasDocumentsIndex()
        {
            Documents = new HashSet<Document>();
        }

        public virtual int Id { get; set; }
        public virtual ICollection<Document> Documents { get; set; }
        public abstract void Describe(DescribeContext context);
    }
}
