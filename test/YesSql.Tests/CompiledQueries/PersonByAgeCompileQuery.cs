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

        public Expression<Func<IQuery, IQuery<Person>>> Query()
        {
            return query => query.For<Person>(false).With<PersonByAge>(x => x.Age == Age || x.Name == Name);
        }
    }
}
