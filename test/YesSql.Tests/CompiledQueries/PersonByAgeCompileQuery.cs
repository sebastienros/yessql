using System;
using System.Linq.Expressions;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.CompiledQueries
{
    public class PersonByNameOrAgeQuery : ICompiledQuery<Person>
    {
        public PersonByNameOrAgeQuery(int age, string name)
        {
            Age = age;
            Name = name;
        }

        public int Age { get; }
        public string Name { get; }

        public Expression<Func<IQuery<Person>, IQuery<Person>>> Query()
        {
            return query => query.With<PersonByAge>(x => x.Age == Age || x.Name == Name);
        }
    }

    public class PersonOrderedAscQuery : ICompiledQuery<Person>
    {
        public Expression<Func<IQuery<Person>, IQuery<Person>>> Query()
        {
            return query => query.With<PersonByAge>().OrderBy(x => x.Age);
        }
    }

    public class PersonOrderedDescQuery : ICompiledQuery<Person>
    {
        public Expression<Func<IQuery<Person>, IQuery<Person>>> Query()
        {
            return query => query.With<PersonByAge>().OrderByDescending(x => x.Age);
        }
    }
}
