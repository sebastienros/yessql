namespace YesSql.Core.Data.Mappings {
    public class StringMaxLengthAttribute : StringLengthAttribute 
    {
        public StringMaxLengthAttribute() : base(4001)
        {
        }
    }
}
