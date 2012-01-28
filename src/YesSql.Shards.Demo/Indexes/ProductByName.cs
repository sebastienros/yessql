using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Shards.Demo.Models;

namespace YesSql.Shards.Demo.Indexes
{
    public class ProductByName : HasDocumentIndex, IIndexProvider
    {
        public virtual string Name { get; set; }

        public virtual void Describe(DescribeContext context) {
            context
                .For<Product, ProductByName>()
                .Index(
                    map: products => products.Select(p => new ProductByName { Name = p.Name })
            );
        }
    }
}
