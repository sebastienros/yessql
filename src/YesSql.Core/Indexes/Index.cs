using System.Collections.Generic;
using YesSql.Core.Data.Models;

namespace YesSql.Core.Indexes {
    public abstract class Index : IIndex {
        public virtual int Id { get; set; }

        public virtual ICollection<Document> Documents {
            get { return new List<Document>(); }
            set { }
        }
    }
}
