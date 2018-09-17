using System;
using System.Linq.Expressions;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.CompiledQueries
{
    public class PersonByAgeQuery : ICompiledQuery<Person>
    {
        public PersonByAgeQuery(int age)
        {
            Age = age;
        }

        public int Age { get; }

        public Expression<Func<IQuery, IQuery<Person>>> Query()
        {
            return query => query.For<Person>(false).With<PersonByAge>(x => x.Age == Age);
        }
    }
}
