using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class ShapeIndex : MapIndex
    {
        public string Name { get; set; }
    }

    public class ShapeIndexProvider<TShape> : IndexProvider<TShape> where TShape : Shape
    {
        public override void Describe(DescribeContext<TShape> context)
        {
            context
                .For<ShapeIndex>()
                .Map(shape => new ShapeIndex { Name = typeof(TShape).Name });
        }
    }
}
