using System;
using System.Linq.Expressions;
using YesSql.Services;
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
            // Because each compiled query has difference caches for each combination of nullable properties,
            // it's possible to return different queries when the nullability of a property changes.
            // for instance:
            // return Name == null
            // ? query.With<PersonByAge>(x => x.Age == Age)
            // : query.With<PersonByAge>(x => x.Age == Age && x.Name == Name);

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

    public class PersonPagedQuery : ICompiledQuery<Person>
    {
        public Expression<Func<IQuery<Person>, IQuery<Person>>> Query()
        {
            return query => query.With<PersonByAge>().Take(1);
        }
    }

    public class PersonByNameColQuery : ICompiledQuery<Person>
    {
        public PersonByNameColQuery(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Expression<Func<IQuery<Person>, IQuery<Person>>> Query()
        {
            return query => query.With<PersonByNameCol>(x => x.Name == Name);
        }
    }

    public class PersonByNamesQuery : ICompiledQuery<Person>
    {
        public PersonByNamesQuery(params string[] names)
        {
            Names = names;
        }

        public string[] Names { get; }

        public Expression<Func<IQuery<Person>, IQuery<Person>>> Query()
        {
            return query => query.With<PersonByName>(x => x.SomeName.IsIn(Names));
        }
    }

    public class PersonByNameNoIndexOptimizationsQuery : ICompiledQuery<Person>
    {
        public PersonByNameNoIndexOptimizationsQuery(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Expression<Func<IQuery<Person>, IQuery<Person>>> Query()
        {
            return query => query.With<PersonByName>(x => x.SomeName == Name).NoIndexOptimizations();
        }
    }

    public class PersonByLiteralNamesQuery : ICompiledQuery<Person>
    {
        public Expression<Func<IQuery<Person>, IQuery<Person>>> Query()
        {
            return query => query.With<PersonByName>(x => x.SomeName.IsIn(new[] { "Bill", "Steve" }));
        }
    }
}
