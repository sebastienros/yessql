using System.Collections.Generic;
using System.Linq;
using YesSql.Core.Data.Models;

namespace YesSql.Core.Indexes
{
    public abstract class MapIndex : IHasDocumentIndex
    {
        private readonly List<Document> _documents = new List<Document>(1);

        public virtual int Id { get; set; }

        public virtual Document Document
        {
            get { return _documents.FirstOrDefault(); }
            set
            {
                _documents.Clear();
                if (value != null)
                {
                    _documents.Add(value);
                }
            }
        }

        ICollection<Document> IIndex.Documents
        {
            get { return _documents; }
        }
    }
}