using System.Runtime.Serialization;

namespace YesSql.Tests.Models
{
    public class Animal
    {
        public Animal()
        { 
        }

        public Animal(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        [IgnoreDataMember]
        public string Color { get; set; }
    }
}
