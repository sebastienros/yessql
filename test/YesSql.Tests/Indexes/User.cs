using System.Collections.Generic;
using System.Linq;
using YesSql.Indexes;

namespace YesSql.Tests.Indexes
{
    public class User
    {
        public string UserName { get; set; }
        public string NormalizedUserName { get; set; }
        public List<string> RoleNames { get; set; } = new List<string>();
    }

    public class UserByRoleNameIndex : ReduceIndex
    {
        public string RoleName { get; set; }
        public int Count { get; set; }
    }

    public class UserByRoleNameIndexProvider : IndexProvider<User>
    {

        public override void Describe(DescribeContext<User> context)
        {
            context.For<UserByRoleNameIndex, string>()
                .Map(user => user.RoleNames.Select(x => new UserByRoleNameIndex
                {
                    RoleName = x,
                    Count = 1
                }))
                .Group(user => user.RoleName)
                .Reduce(group => new UserByRoleNameIndex
                {
                    RoleName = group.Key,
                    Count = group.Sum(x => x.Count)
                })
                .Delete((index, map) =>
                {
                    index.Count -= map.Sum(x => x.Count);
                    return index.Count > 0 ? index : null;
                });
        }
    }
}
