using System;
using System.Collections.Generic;
using System.Text;

namespace YesSql.Tests.Models
{
    public class Tree
    {
        public int Id { get; private set; }

        public string Type { get; set; }

        public int HeightInInches { get; set; }
    }
}
