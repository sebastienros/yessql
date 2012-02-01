using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Samples.Shards.Models;

namespace YesSql.Samples.Shards.Indexes
{
    public class ProductByName : MapIndex
    {
        public virtual string Name { get; set; }
    }

    public class ProductIndexProvider : IndexProvider<Product>
    {
        public override void Describe(DescribeContext<Product> context) {
            context
                .For<ProductByName>()
                .Index(
                    map: products => products.Select(p => new ProductByName { Name = p.Name })
            );
        }
        
    }
}
