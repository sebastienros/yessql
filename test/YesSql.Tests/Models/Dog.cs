namespace YesSql.Tests.Models
{
    public class Dog : Animal
    {
        public Dog() : base("dog")
        { 
        }

        public string Breed { get; set; }
    }
}
