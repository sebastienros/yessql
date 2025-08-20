using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YesSql.Tests.Models
{
    public class Drawing
    {
        public Drawing()
        {
            Shapes = new List<Shape>();
        }

        public IList<Shape> Shapes { get; set; }
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(Square), nameof(Square))]
    [JsonDerivedType(typeof(Circle), nameof(Circle))]
    public abstract class Shape
    {
        public long Id { get; set; }
    }

    public class Square : Shape
    {
        public int Size { get; set; }
    }

    public class Circle : Shape
    {
        public int Radius { get; set; }
    }
}
