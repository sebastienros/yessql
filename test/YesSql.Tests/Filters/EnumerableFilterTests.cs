using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YesSql.Filters.Enumerable;
using YesSql.Tests.Models;

namespace YesSql.Tests.Filters
{
    public class ArticleDocument
    {
        public List<Article> Articles { get; } = new();
    }

    public class EnumerableFilterTests
    {
        [Fact]
        public async Task ShouldParseNamedTermQuery()
        {
            var document = new ArticleDocument();
            var billsArticle = new Article
            {
                Title = "article by bill about rabbits",
                PublishedUtc = DateTime.UtcNow
            };

            var stevesArticle = new Article
            {
                Title = "post by steve about cats",
                PublishedUtc = DateTime.UtcNow
            };

            document.Articles.Add(billsArticle);
            document.Articles.Add(stevesArticle);


            var filter = "title:steve";

            var toFilter = document.Articles.AsEnumerable();

            var parser = new EnumerableEngineBuilder<Article>()
                .WithNamedTerm("title", b => b
                    .OneCondition((val, enumerable) => enumerable.Where(x => x.Title.Contains(val, StringComparison.OrdinalIgnoreCase)))
                )
                .Build();

            var parsed = parser.Parse(filter);

            var filtered = await parsed.ExecuteAsync(toFilter);

            // Parsed query
            Assert.Equal("post by steve about cats", filtered.FirstOrDefault().Title);
            Assert.Single(filtered);
        }

        [Theory]
        [InlineData("steve")]
        [InlineData("title:steve")]
        public async Task ShouldParseDefaultTermQuery(string search)
        {
            var document = new ArticleDocument();

            var billsArticle = new Article
            {
                Title = "article by bill about rabbits",
                PublishedUtc = DateTime.UtcNow
            };

            var stevesArticle = new Article
            {
                Title = "post by steve about cats",
                PublishedUtc = DateTime.UtcNow
            };

            document.Articles.Add(billsArticle);
            document.Articles.Add(stevesArticle);

            var toQuery = document.Articles.AsEnumerable();

            var parser = new EnumerableEngineBuilder<Article>()
                .WithDefaultTerm("title", b => b
                    .OneCondition((val, query) => query.Where(x => x.Title.Contains(val, StringComparison.OrdinalIgnoreCase))))
                .Build();

            var parsed = parser.Parse(search);

            var filtered = await parsed.ExecuteAsync(toQuery);

            var assert3 = filtered.FirstOrDefault().Title;
            var assert4 = filtered.Count();

            // Parsed query
            Assert.Equal("post by steve about cats", assert3);
            Assert.Equal(1, assert4);
        }

        [Fact]
        public async Task ShouldParseOrQuery()
        {
            var document = new ArticleDocument();

            var billsArticle = new Article
            {
                Title = "article by bill about rabbits",
                PublishedUtc = DateTime.UtcNow
            };

            var stevesArticle = new Article
            {
                Title = "post by steve about cats",
                PublishedUtc = DateTime.UtcNow
            };

            document.Articles.Add(billsArticle);
            document.Articles.Add(stevesArticle);

            // boolean OR "title:(bill OR post)"
            var filter = "title:bill post";
            var filterQuery = document.Articles.AsEnumerable();

            var parser = new EnumerableEngineBuilder<Article>()
                .WithNamedTerm("title", b => b
                    .ManyCondition(
                        (val, query) => query.Where(x => x.Title.Contains(val, StringComparison.OrdinalIgnoreCase)),
                        (val, query) => query.Where(x => !x.Title.Contains(val, StringComparison.OrdinalIgnoreCase))
                    )
                )
                .Build();

            var parsed = parser.Parse(filter);
            var filtered = await parsed.ExecuteAsync(filterQuery);

            // Parsed query
            Assert.Equal(2, filtered.Count());
        }

        [Fact]
        public async Task ShouldParseAndQuery()
        {
            var document = new ArticleDocument();

            var billsArticle = new Article
            {
                Title = "article by bill about rabbits",
                PublishedUtc = DateTime.UtcNow
            };

            var stevesArticle = new Article
            {
                Title = "post by steve about cats",
                PublishedUtc = DateTime.UtcNow
            };

            document.Articles.Add(billsArticle);
            document.Articles.Add(stevesArticle);

            var toFilter = document.Articles.AsEnumerable();

            // boolean AND "title:(bill AND rabbits)"
            var filter = "title:bill AND rabbits";

            var parser = new EnumerableEngineBuilder<Article>()
                .WithNamedTerm("title", b => b
                    .ManyCondition(
                        (val, query) => query.Where(x => x.Title.Contains(val, StringComparison.OrdinalIgnoreCase)),
                        (val, query) => query.Where(x => !x.Title.Contains(val, StringComparison.OrdinalIgnoreCase))
                    )
                )
                .Build();

            var parsed = parser.Parse(filter);

            var filtered = await parsed.ExecuteAsync(toFilter);

            // Parsed query
            Assert.Single(filtered);
        }

        [Fact]
        public async Task ShouldParseTwoNamedTermQuerys()
        {
            var document = new ArticleDocument();

            var billsArticle = new Article
            {
                Title = "article by bill about rabbits",
                PublishedUtc = DateTime.UtcNow
            };

            var stevesArticle = new Article
            {
                Title = "article by steve about cats",
                PublishedUtc = DateTime.UtcNow
            };

            document.Articles.Add(billsArticle);
            document.Articles.Add(stevesArticle);

            var filter = "title:article title:article";
            var filterQuery = document.Articles.AsEnumerable();

            var parser = new EnumerableEngineBuilder<Article>()
                .WithNamedTerm("title", b => b
                    .OneCondition((val, query) => query.Where(x => x.Title.Contains(val, StringComparison.OrdinalIgnoreCase)))
                    .AllowMultiple()
                )
                .Build();

            var parsed = parser.Parse(filter);

            var filtered = await parsed.ExecuteAsync(filterQuery);

            // Parsed query
            Assert.Equal(2, filterQuery.Count());
        }

        [Fact]
        public async Task ShouldParseComplexQuery()
        {
            var document = new ArticleDocument();
            var beachLizardsArticle = new Article
            {
                Title = "On the beach in the sand we found lizards",
                PublishedUtc = DateTime.UtcNow
            };

            var mountainArticle = new Article
            {
                Title = "On the mountain it snowed at the lake",
                PublishedUtc = DateTime.UtcNow
            };

            document.Articles.Add(beachLizardsArticle);
            document.Articles.Add(mountainArticle);


            var filter = "title:(beach AND sand) OR (mountain AND lake)";
            var filterQuery = document.Articles.AsEnumerable();

            var parser = new EnumerableEngineBuilder<Article>()
                .WithNamedTerm("title", b => b
                    .ManyCondition(
                        (val, query) => query.Where(x => x.Title.Contains(val, StringComparison.OrdinalIgnoreCase)),
                        (val, query) => query.Where(x => !x.Title.Contains(val, StringComparison.OrdinalIgnoreCase))
                    )
                )
                .Build();

            var parsed = parser.Parse(filter);

            await parsed.ExecuteAsync(filterQuery);

            // Parsed query
            Assert.Equal(2, filterQuery.Count());
        }

        [Fact]
        public async Task ShouldParseNotComplexQuery()
        {
            var document = new ArticleDocument();

            var beachLizardsArticle = new Article
            {
                Title = "On the beach in the sand we found lizards",
                PublishedUtc = DateTime.UtcNow
            };

            var sandcastlesArticle = new Article
            {
                Title = "On the beach in the sand we built sandcastles",
                PublishedUtc = DateTime.UtcNow
            };

            var mountainArticle = new Article
            {
                Title = "On the mountain it snowed at the lake",
                PublishedUtc = DateTime.UtcNow
            };

            document.Articles.Add(beachLizardsArticle);
            document.Articles.Add(sandcastlesArticle);
            document.Articles.Add(mountainArticle);

            // boolean : ((beach AND sand) OR (mountain AND lake)) NOT lizards 
            var filter = "title:((beach AND sand) OR (mountain AND lake)) NOT lizards";
            var toFilter = document.Articles.AsEnumerable();

            var parser = new EnumerableEngineBuilder<Article>()
                .WithNamedTerm("title", b => b
                    .ManyCondition(
                        (val, query) => query.Where(x => x.Title.Contains(val, StringComparison.OrdinalIgnoreCase)),
                        (val, query) => query.Where(x => !x.Title.Contains(val, StringComparison.OrdinalIgnoreCase))
                    )
                )
                .Build();

            var parsed = parser.Parse(filter);

            var filtered = await parsed.ExecuteAsync(toFilter);

            // Parsed query
            Assert.Equal(2, filtered.Count());
        }

        [Fact]
        public async Task ShouldParseNotBooleanQuery()
        {
            var document = new ArticleDocument();

            var billsArticle = new Article
            {
                Title = "Article by bill about rabbits",
                PublishedUtc = DateTime.UtcNow
            };

            var stevesArticle = new Article
            {
                Title = "Post by steve about cats",
                PublishedUtc = DateTime.UtcNow
            };

            var paulsArticle = new Article
            {
                Title = "Blog by paul about chickens",
                PublishedUtc = DateTime.UtcNow
            };

            document.Articles.Add(billsArticle);
            document.Articles.Add(stevesArticle);
            document.Articles.Add(paulsArticle);

            var filter = "title:NOT steve";

            var toFilter = document.Articles.AsEnumerable();

            var parser = new EnumerableEngineBuilder<Article>()
                .WithNamedTerm("title", b => b
                    .ManyCondition(
                        (val, query) => query.Where(x => x.Title.Contains(val, StringComparison.OrdinalIgnoreCase)),
                        (val, query) => query.Where(x => !x.Title.Contains(val, StringComparison.OrdinalIgnoreCase))
                    )
                )
                .Build();

            var parsed = parser.Parse(filter);

            var filtered = await parsed.ExecuteAsync(toFilter);

            // Parsed query
            Assert.Equal(2, filtered.Count());
        }

        [Fact]
        public async Task ShouldParseNotQueryWithOrder()
        {
            var document = new ArticleDocument();

            var billsArticle = new Article
            {
                Title = "Article by bill about rabbits",
                PublishedUtc = DateTime.UtcNow
            };

            var stevesArticle = new Article
            {
                Title = "Post by steve about cats",
                PublishedUtc = DateTime.UtcNow
            };

            var paulsArticle = new Article
            {
                Title = "Blog by paul about chickens",
                PublishedUtc = DateTime.UtcNow
            };

            document.Articles.Add(billsArticle);
            document.Articles.Add(stevesArticle);
            document.Articles.Add(paulsArticle);

            var filter = "title:about NOT steve";
            var filterQuery = document.Articles.AsEnumerable();

            var parser = new EnumerableEngineBuilder<Article>()
                .WithNamedTerm("title", b => b
                    .ManyCondition(
                        (val, query) => query.Where(x => x.Title.Contains(val, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(x => x.Title),
                        (val, query) => query
                            .Where(x => !x.Title.Contains(val, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(x => x.Title)
                    )
                )
                .Build();

            var parsed = parser.Parse(filter);

            var filtered = await parsed.ExecuteAsync(filterQuery);

            // Parsed query
            Assert.Equal(2, filtered.Count());
            Assert.Equal("Blog by paul about chickens", filtered.FirstOrDefault().Title);
        }
    }
}
