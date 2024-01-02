using System;
using System.Collections.Generic;
using System.Linq;
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
        public static Dictionary<string, Type> IndexTypeCache = new Dictionary<string, Type>();
        public override void Describe(DescribeContext<Property> context)
        {
            foreach (var dynamicType in IndexTypeCache.Values)
            {
                context
                 .For(dynamicType)
                 .Map(property =>
                 {
                     // It's just a test. We only have one type
                     var obj = Activator.CreateInstance(dynamicType);
                     foreach (var prop in dynamicType.GetProperties())
                     {
                         switch (prop.Name)
                         {
                             case "Name":
                                 prop.SetValue(obj, property.Name);
                                 break;
                             case "ForRent":
                                 prop.SetValue(obj, property.ForRent);
                                 break;
                             case "IsOccupied":
                                 prop.SetValue(obj, property.IsOccupied);
                                 break;
                             case "Location":
                                 prop.SetValue(obj, property.Location);
                                 break;
                         }
                     }

                     return (MapIndex)obj;
                 });
            }
        }
    }
}
