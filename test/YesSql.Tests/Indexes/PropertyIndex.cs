using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PropertyIndex : MapIndex
    {
        public string Name { get; set; }
        public bool ForRent { get; set; }
        public bool IsOccupied { get; set; }
        public string Location { get; set; }
    }

    public class PropertyIndexProvider : IndexProvider<Property>
    {
        public override void Describe(DescribeContext<Property> context)
        {
            context
                .For<PropertyIndex>()
                .Map(property => new PropertyIndex
                {
                    Name = property.Name,
                    ForRent = property.ForRent,
                    IsOccupied = property.IsOccupied,
                    Location = property.Location
                });
        }
    }
    
    public class PropertyDynamicIndexProvider : IndexProvider<Property>
    {
        public override void Describe(DescribeContext<Property> context)
        {
            var propertyType = typeof(PropertyIndex);
            context
                .For(propertyType)
                .Map(property => new PropertyIndex
                {
                    Name = property.Name,
                    ForRent = property.ForRent,
                    IsOccupied = property.IsOccupied,
                    Location = property.Location
                });
        }
    }
}
