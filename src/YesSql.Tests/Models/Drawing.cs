using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YesSql.Tests.Models {
    public class Drawing 
    {
        public Drawing() 
        {
            Shapes = new List<Shape>();
        }

        public IList<Shape> Shapes { get; set; }
    }

    public abstract class Shape 
    {
        public int Id { get; set; }
    }

    public class Square : Shape
    {
        public int Size { get; set;}
    }

    public class Circle : Shape
    {
        public int Radius { get; set; }
    }
}
