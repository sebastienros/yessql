using YesSql.Indexes;

namespace YesSql.Samples.Performance
{
    public class User
    {
        //public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
    }
        
    public class UserByName : MapIndex
    {
        public string Name { get; set; }
    }

    public class UserIndexProvider : IndexProvider<User>
    {
        public override void Describe(DescribeContext<User> context)
        {
            context.For<UserByName>()
                .Map(user => new UserByName { Name = user.Name });
        }
    }
}
