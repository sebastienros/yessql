using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace YesSql.Tests.Models {
    public class Animal 
    {
        public string Name { get; set; }
        
        [IgnoreDataMember]
        public string Color { get; set; }
    }
}
