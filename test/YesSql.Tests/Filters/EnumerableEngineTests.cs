using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using YesSql.Filters.Enumerable;
using YesSql.Tests.Models;

namespace YesSql.Tests.Filters
{
    public class EnumerableEngineTests
    {
        [Fact]
        public void ShouldParseNamedTerm()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("name", b => b.OneCondition(PersonOneConditionQuery()))
                .Build();

            Assert.Equal("name:steve", parser.Parse("name:steve").ToString());
            Assert.Equal("name:steve", parser.Parse("name:steve").ToNormalizedString());
        }

        [Fact]
        public void ShouldParseNamedTermWhenQuoted()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("name", b => b.OneCondition(PersonOneConditionQuery()))
                .Build();

            Assert.Equal("name:\"steve balmer\"", parser.Parse("name:\"steve balmer\"").ToString());
            Assert.Equal("name:\"steve balmer\"", parser.Parse("name:\"steve balmer\"").ToNormalizedString());
            Assert.Equal("name:'steve balmer'", parser.Parse("name:'steve balmer'").ToString());
            Assert.Equal("name:'steve balmer'", parser.Parse("name:'steve balmer'").ToNormalizedString());
        }

        [Fact]
        public void ShouldParseTermWithLocalizedChars()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("name", b => b.OneCondition(PersonOneConditionQuery()))
                .Build();

            Assert.Equal("name:账单", parser.Parse("name:账单").ToString());
            Assert.Equal("name:账单", parser.Parse("name:账单").ToNormalizedString());
        }        

        [Fact]
        public void ShouldParseManyNamedTerms()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("name", b => b.OneCondition(PersonOneConditionQuery()))
                .WithNamedTerm("status", b => b.OneCondition(PersonOneConditionQuery()))
                .Build();

            Assert.Equal("name:steve status:published", parser.Parse("name:steve status:published").ToString());
            Assert.Equal("name:steve status:published", parser.Parse("name:steve status:published").ToNormalizedString());
        }

        [Fact]
        public void ShouldParseManyNamedTermsWithManyCondition()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("name", b => b
                    .ManyCondition(PersonManyMatch(), PersonManyNotMatch()))
                .WithNamedTerm("status", b => b
                    .ManyCondition(PersonManyMatch(), PersonManyNotMatch()))
                .Build();

            Assert.Equal("name:steve status:published", parser.Parse("name:steve status:published").ToString());
            Assert.Equal("name:steve status:published", parser.Parse("name:steve status:published").ToNormalizedString());
        }

        [Fact]
        public void ShouldParseDefaultTermWithManyCondition()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithDefaultTerm("name", b => b.ManyCondition(PersonManyMatch(), PersonManyNotMatch()))
                .WithNamedTerm("status", b => b.ManyCondition(PersonManyMatch(), PersonManyNotMatch()))
                .Build();

            Assert.Equal("steve status:published", parser.Parse("steve status:published").ToString());
            Assert.Equal("name:steve status:published", parser.Parse("steve status:published").ToNormalizedString());
        }

        [Fact]
        public void ShouldParseDefaultTermWithManyConditionWhenLast()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("status", b => b.ManyCondition(PersonManyMatch(), PersonManyNotMatch()))
                .WithDefaultTerm("name", b => b.ManyCondition(PersonManyMatch(), PersonManyNotMatch()))
                .Build();

            Assert.Equal("steve status:published", parser.Parse("steve status:published").ToString());
            Assert.Equal("name:steve status:published", parser.Parse("steve status:published").ToNormalizedString());
        }

        [Fact]
        public void ShouldParseDefaultTermWithManyConditionWhenDefaultIsFirst()
        {
            // TODO Validation on builder if you have two manys. you cannot have a default.
            var parser = new EnumerableEngineBuilder<Person>()
                .WithDefaultTerm("name", b => b.ManyCondition(PersonManyMatch(), PersonManyNotMatch()))
                .WithNamedTerm("status", b => b.ManyCondition(PersonManyMatch(), PersonManyNotMatch()))
                .Build();

            Assert.Equal("status:(published OR steve)", parser.Parse("status:published steve").ToNormalizedString());
        }

        [Fact]
        public void ShouldParseDefaultTerm()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("age", b => b.OneCondition(PersonOneConditionQuery()))
                .WithDefaultTerm("name", b => b.OneCondition(PersonOneConditionQuery()))
                .Build();

            Assert.Equal("name:steve", parser.Parse("name:steve").ToString());
            Assert.Equal("steve", parser.Parse("steve").ToString());
            Assert.Equal("steve age:20", parser.Parse("steve age:20").ToString());
            Assert.Equal("age:20 name:steve", parser.Parse("age:20 name:steve").ToString());
            Assert.Equal("age:20 steve", parser.Parse("age:20 steve").ToString());
            Assert.Equal(2, parser.Parse("steve age:20").Count());
            Assert.Equal("name:steve", parser.Parse("steve").ToNormalizedString());
        }

        [Fact]
        public void ShouldParseDefaultTermWithOneMany()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("age", builder => builder.OneCondition(PersonOneConditionQuery()))
                .WithDefaultTerm("name", builder =>
                    builder.ManyCondition(PersonManyMatch(), PersonManyNotMatch())
                )
                .Build();


            Assert.Equal("name:steve", parser.Parse("name:steve").ToString());
            Assert.Equal("steve", parser.Parse("steve").ToString());
            Assert.Equal("steve age:20", parser.Parse("steve age:20").ToString());
            Assert.Equal("age:20 name:steve", parser.Parse("age:20 name:steve").ToString());
            Assert.Equal("age:20 steve", parser.Parse("age:20 steve").ToString());
            Assert.Equal(2, parser.Parse("steve age:20").Count());
            Assert.Equal("name:steve", parser.Parse("steve").ToNormalizedString());
        }

        [Fact]
        public void ShouldParseDefaultTermAtEndOfStatement()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("age", b => b
                    .OneCondition((val, query) =>
                    {
                        if (Int32.TryParse(val, out var age))
                        {
                            query.Where(x => x.Age == age);
                        }

                        return query;
                    }))
                .WithDefaultTerm("name", b => b.OneCondition(PersonOneConditionQuery()))
                .Build();


            Assert.Equal("age:20 name:steve", parser.Parse("age:20 name:steve").ToString());
            Assert.Equal(2, parser.Parse("age:20 name:steve").Count());
            Assert.Equal("age:20 steve", parser.Parse("age:20 steve").ToString());
            Assert.Equal(2, parser.Parse("age:20 steve").Count());
        }

        [Fact]
        public void ShouldParseDefaultTermAtEndOfStatementWithBuilder()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("age", builder =>
                    builder
                        .OneCondition((val, query) =>
                        {
                            if (Int32.TryParse(val, out var age))
                            {
                                query.Where(x => x.Age == age);
                            }

                            return query;
                        })
                )
                .WithDefaultTerm("name", builder =>
                    builder.OneCondition(PersonOneConditionQuery())
                )
                .Build();

            Assert.Equal("age:20 name:steve", parser.Parse("age:20 name:steve").ToString());
            Assert.Equal(2, parser.Parse("age:20 name:steve").Count());
            Assert.Equal("age:20 steve", parser.Parse("age:20 steve").ToString());
            Assert.Equal(2, parser.Parse("age:20 steve").Count());
        }

        [Fact]
        public void OrderOfDefaultTermShouldNotMatter()
        {
            var parser1 = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("age", b => b.OneCondition(PersonOneConditionQuery()))
                .WithDefaultTerm("name", b => b.ManyCondition(PersonManyMatch(), PersonManyNotMatch()))
                .Build();

            var parser2 = new EnumerableEngineBuilder<Person>()
                .WithDefaultTerm("name", b => b.ManyCondition(PersonManyMatch(), PersonManyNotMatch()))
                .WithNamedTerm("age", b => b.OneCondition(PersonOneConditionQuery()))
                .Build();

            Assert.Equal("steve age:20", parser1.Parse("steve age:20").ToString());

            var result = parser1.Parse("steve age:20");
            Assert.Equal(2, result.Count());

            Assert.Equal("age:20 steve", parser1.Parse("age:20 steve").ToString());
            Assert.Equal(2, parser1.Parse("age:20 steve").Count());

            Assert.Equal("steve age:20", parser2.Parse("steve age:20").ToString());
            Assert.Equal(2, parser2.Parse("steve age:20").Count());

            Assert.Equal("age:20 steve", parser2.Parse("age:20 steve").ToString());
            Assert.Equal(2, parser2.Parse("age:20 steve").Count());
        }

        [Theory]
        [InlineData("title:bill post", "title:(bill OR post)")]
        [InlineData("title:bill OR post", "title:(bill OR post)")]
        [InlineData("title:beach AND sand", "title:(beach AND sand)")]
        [InlineData("title:beach AND sand OR mountain AND lake", "title:((beach AND sand) OR (mountain AND lake))")]
        [InlineData("title:(beach AND sand) OR (mountain AND lake)", "title:((beach AND sand) OR (mountain AND lake))")]
        [InlineData("title:(beach AND sand) OR (mountain AND lake) NOT lizards", "title:(((beach AND sand) OR (mountain AND lake)) NOT lizards)")]
        [InlineData("title:NOT beach", "title:NOT beach")]
        [InlineData("title:beach NOT mountain", "title:(beach NOT mountain)")]
        [InlineData("title:beach NOT mountain lake", "title:((beach NOT mountain) OR lake)")]
        public void Complex(string search, string normalized)
        {
            var parser = new EnumerableEngineBuilder<Article>()
                .WithNamedTerm("title", b => b.ManyCondition(ArticleManyMatch(), ArticleManyNotMatch()))
                .Build();

            var result = parser.Parse(search);

            Assert.Equal(normalized, result.ToNormalizedString());
        }

        [Theory]
        [InlineData("title:(bill)", "title:(bill)")]
        [InlineData("title:(bill AND steve) OR Paul", "title:((bill AND steve) OR Paul)")]
        [InlineData("title:((bill AND steve) OR Paul)", "title:((bill AND steve) OR Paul)")]
        public void ShouldGroup(string search, string normalized)
        {
            var parser = new EnumerableEngineBuilder<Article>()
                .WithNamedTerm("title", b => b.ManyCondition(ArticleManyMatch(), ArticleManyNotMatch()))
                .Build();

            var result = parser.Parse(search);

            Assert.Equal(search, result.ToString());
            Assert.Equal(normalized, result.ToNormalizedString());
        }

        [Theory]
        [InlineData("title:bill steve")]
        public void ShouldNotIncludeExtraWhitespace(string search)
        {
            var parser = new EnumerableEngineBuilder<Article>()
                .WithNamedTerm("title", b => b.ManyCondition(ArticleManyMatch(), ArticleManyNotMatch()))
                .Build();

            var result = parser.Parse(search);

            Assert.Equal(search, result.ToString());
        }

        [Fact]
        public void ShouldIgnoreMultipleNamedTerms()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("name", b => b.OneCondition(PersonOneConditionQuery()))
                .Build();

            // By convention the last term is used when single = true;
            Assert.Equal("name:bill", parser.Parse("name:steve name:bill").ToString());
            Assert.Equal("name:bill", parser.Parse("name:steve name:bill").ToNormalizedString());
        }

        [Fact]
        public void ShouldAllowMultipleNamedTerms()
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("name", b => b
                    .OneCondition(PersonOneConditionQuery())
                    .AllowMultiple())
                .Build();

            // By convention the last term is used when single = true;
            Assert.Equal("name:steve name:bill", parser.Parse("name:steve name:bill").ToString());
            Assert.Equal("name:steve name:bill", parser.Parse("name:steve name:bill").ToNormalizedString());
        }

        [Theory]
        [InlineData("extrachar:age-asc")]
        [InlineData("extrachar:age-desc")]
        [InlineData("extrachar:2020-01-01..2020-10-10")]
        [InlineData("extrachar:>ten")]
        [InlineData("extrachar:<100")]
        [InlineData("extrachar:<=100")]
        [InlineData("extrachar:100*")]
        [InlineData("extrachar:100+")]
        public void ShouldIncludeExtraChars(string search)
        {
            var parser = new EnumerableEngineBuilder<Person>()
                .WithNamedTerm("extrachar", b => b.OneCondition(PersonOneConditionQuery()))
                .Build();

            var result = parser.Parse(search);

            Assert.Equal(search, result.ToString());
        } 

        private static Func<string, IEnumerable<Person>, IEnumerable<Person>> PersonOneConditionQuery()
            => (val, enumerable) => enumerable.Where(x => x.Firstname.Contains(val, StringComparison.OrdinalIgnoreCase));

        private static Func<string, IEnumerable<Person>, IEnumerable<Person>> PersonManyMatch()
            => PersonOneConditionQuery();

        private static Func<string, IEnumerable<Person>, IEnumerable<Person>> PersonManyNotMatch()
            => (val, query) => query.Where(x => !x.Firstname.Contains(val, StringComparison.OrdinalIgnoreCase));

        private static Func<string, IEnumerable<Article>, IEnumerable<Article>> ArticleManyMatch()
            => (val, query) => query.Where(x => x.Title.Contains(val, StringComparison.OrdinalIgnoreCase));

        private static Func<string, IEnumerable<Article>, IEnumerable<Article>> ArticleManyNotMatch()
            => (val, query) => query.Where(x => !x.Title.Contains(val, StringComparison.OrdinalIgnoreCase));
    }
}
