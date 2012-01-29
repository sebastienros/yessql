using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Samples.Shards.Models;

namespace YesSql.Samples.Shards.Indexes
{
    public class ProductByName : HasDocumentIndex
    {
        public virtual string Name { get; set; }

        public override void Describe(DescribeContext context) {
            context
                .For<Product, ProductByName>()
                .Index(
                    map: products => products.Select(p => new ProductByName { Name = p.Name })
            );
        }
    }
}
