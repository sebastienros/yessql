using System;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using Xunit;
using YesSql.Core.Data;
using YesSql.Core.Data.Models;
using YesSql.Core.Services;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    public class CoreTests : IDisposable
    {
        private readonly IStore _store;

        public CoreTests()
        {

            // delete the db before starting tests
            if (File.Exists("Store.sdf"))
            {
                File.Delete("Store.sdf");
            }

            // recreating a fresh SqlCe db
            new SqlCeEngine {LocalConnectionString = "Data Source=Store.sdf"}.CreateDatabase();

            _store = new Store().Configure(MsSqlCeConfiguration.MsSqlCe40.ConnectionString("Data Source=Store.sdf").ShowSql());

        }

        public void Dispose()
        {
            _store.Dispose();
        }

        [Fact]
        public void ShouldCreateDatabase()
        {

            using (var session = _store.CreateSession())
            {
                var doc = new Document {Type = "Product", Content = "{}"};

                session.Save(doc);
                session.Commit();
            }
        }

        [Fact]
        public void ShouldSaveCustomObject()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                session.Save(bill);
                session.Commit();
            }
        }

        [Fact]
        public void ShouldSaveSeveralObjects()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                var steve = new Person
                {
                    Firstname = "Steve",
                    Lastname = "Balmer"
                };

                session.Save(bill);
                session.Save(steve);
                session.Commit();
            }
        }

        [Fact]
        public void ShouldSaveAnonymousObject()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                var steve = new
                {
                    Firstname = "Steve",
                    Lastname = "Balmer"
                };

                session.Save(bill);
                session.Save(steve);
                session.Commit();
            }
        }

        [Fact]
        public void ShouldLoadAnonymousDocument()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Address = new
                    {
                        Street = "1 Microsoft Way",
                        City = "Redmond"
                    }
                };

                session.Save(bill);
                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                dynamic person = session.QueryDocument().FirstOrDefault().As<object>();

                Assert.NotNull(person);
                Assert.Equal("Bill", person.Firstname);
                Assert.Equal("Gates", person.Lastname);

                Assert.NotNull(person.Address);
                Assert.Equal("1 Microsoft Way", person.Address.Street);
                Assert.Equal("Redmond", person.Address.City);
            }
        }

        [Fact]
        public void ShouldSerializeComplexObject()
        {
            using (var session = _store.CreateSession())
            {
                var product = new Product
                {
                    Cost = 3.99m,
                    Name = "Milk",
                };

                session.Save(product);
                session.Commit();

                session.Save(new Order
                {
                    Customer = "customers/microsoft",
                    OrderLines =
                        {
                            new OrderLine
                            {
                                ProductId = product.Id,
                                Quantity = 3
                            },
                        }
                });
                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                var order = session.QueryDocument<Order>(q => q.FirstOrDefault());
                Assert.NotNull(order);
                Assert.Equal(1, order.OrderLines.Count);

                var prod =
                    session.QueryDocument<Product>(q => q.FirstOrDefault(x => x.Id == order.OrderLines[0].ProductId));
                Assert.NotNull(prod);
                Assert.Equal("Milk", prod.Name);
            }
        }

        [Fact]
        public void ShouldCreateIndexAndLinkToDocument()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                };

                var steve = new Person
                {
                    Firstname = "Steve",
                    Lastname = "Balmer"
                };

                session.Save(bill);
                session.Save(steve);
                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, session.QueryIndex<PersonByName>().Count());
                Assert.Equal(1, session.QueryIndex<PersonByName>().Count(x => x.Name == "Bill"));
                Assert.Equal(1, session.QueryIndex<PersonByName>().Count(x => x.Name == "Steve"));
                Assert.Equal(0, session.QueryIndex<PersonByName>().Count(x => x.Name == "Joe"));

                var person =
                    session.QueryByMappedIndex<PersonByName, Person>(q => q.FirstOrDefault(x => x.Name == "Bill"));
                Assert.NotNull(person);
                Assert.Equal("Bill", person.Firstname);
            }
        }

        [Fact]
        public void ShouldReduce()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var dates = new[]
                {
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 3),
                    new DateTime(2011, 11, 4),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 3),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 1)
                };

                var articles = dates.Select(x => new Article
                {
                    PublishedUtc = x
                });

                foreach (var article in articles)
                {
                    session.Save(article);
                }

                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(4, session.QueryIndex<ArticlesByDay>().Count());

                Assert.Equal(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear));
                Assert.Equal(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear));
                Assert.Equal(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear));
                Assert.Equal(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear));

                Assert.Equal(4,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).Count);
                Assert.Equal(3,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).Count);
                Assert.Equal(2,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).Count);
                Assert.Equal(1,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).Count);
            }
        }

        [Fact]
        public void ShouldReduceAndMergeWithDatabase()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                session.Save(new ArticlesByDay {Count = 1, DayOfYear = new DateTime(2011, 11, 1).DayOfYear});
                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                var dates = new[]
                {
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 3),
                    new DateTime(2011, 11, 4),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 3),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 1)
                };

                var articles = dates.Select(x => new Article
                {
                    PublishedUtc = x
                });

                foreach (var article in articles)
                {
                    session.Save(article);
                }

                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(4, session.QueryIndex<ArticlesByDay>().Count());

                Assert.Equal(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear));
                Assert.Equal(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear));
                Assert.Equal(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear));
                Assert.Equal(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear));

                Assert.Equal(5,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).Count);
                Assert.Equal(3,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).Count);
                Assert.Equal(2,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).Count);
                Assert.Equal(1,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).Count);
            }
        }

        [Fact]
        public void MultipleIndexesShoudNotConflict() {
            _store.RegisterIndexes<ArticleIndexProvider>();
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession()) {
                session.Save(new ArticlesByDay { Count = 1, DayOfYear = new DateTime(2011, 11, 1).DayOfYear });
                session.Commit();
            }

            using (var session = _store.CreateSession()) {
                session.Save(new Article {
                    PublishedUtc = new DateTime(2011, 11, 1)
                });
            
                session.Commit();
            }

            using (var session = _store.CreateSession()) {
                Assert.Equal(1, session.QueryIndex<ArticlesByDay>().Count());
            }
        }

        [Fact]
        public void ShouldDeleteCustomObject()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            var bill = new Person
            {
                Firstname = "Bill",
                Lastname = "Gates"
            };

            using (var session = _store.CreateSession())
            {
                session.Save(bill);
                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                var person = session.QueryDocument<Person>(q => q.FirstOrDefault());
                Assert.NotNull(person);

                session.Delete(person);
                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                var person = session.QueryDocument<Person>(q => q.FirstOrDefault());
                Assert.Null(person);
            }
        }

        [Fact]
        public void RemovingDocumentShouldDeleteMappedIndex()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            var bill = new Person
            {
                Firstname = "Bill",
                Lastname = "Gates"
            };

            using (var session = _store.CreateSession())
            {
                session.Save(bill);
                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                var personByName = session.QueryIndex<PersonByName>().FirstOrDefault();
                Assert.NotNull(personByName);

                var person = session.QueryDocument<Person>(q => q.FirstOrDefault());
                Assert.NotNull(person);

                session.Delete(person);
                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                var personByName = session.QueryIndex<PersonByName>().FirstOrDefault();
                Assert.Null(personByName);
            }
        }

        [Fact]
        public void RemovingDocumentShouldDeleteReducedIndex()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var dates = new[]
                {
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 3),
                    new DateTime(2011, 11, 4),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 3),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 1)
                };

                var articles = dates.Select(x => new Article
                {
                    PublishedUtc = x
                });

                foreach (var article in articles)
                {
                    session.Save(article);
                }

                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(10, session.QueryDocument<Article>().Count());
                Assert.Equal(4, session.QueryIndex<ArticlesByDay>().Count());
            }

            // delete a document
            using (var session = _store.CreateSession())
            {
                var article =
                    session.QueryByReducedIndex<ArticlesByDay, Article>(
                        a => a.Where(b => b.DayOfYear == new DateTime(2011, 11, 4).DayOfYear)).FirstOrDefault();
                Assert.NotNull(article);
                session.Delete(article);

                session.Commit();
            }

            // there should be only 3 indexes left
            using (var session = _store.CreateSession())
            {
                // document was deleted
                Assert.Equal(9, session.QueryDocument<Article>().Count()); 
                // index was deleted
                Assert.Equal(3, session.QueryIndex<ArticlesByDay>().Count()); 
            }
        }

        [Fact]
        public void UpdatingDocumentShouldUpdateReducedIndex()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var dates = new[]
                {
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 3),
                    new DateTime(2011, 11, 4),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 3),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 1)
                };

                var articles = dates.Select(x => new Article
                {
                    PublishedUtc = x
                });

                foreach (var article in articles)
                {
                    session.Save(article);
                }

                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(10, session.QueryDocument<Article>().Count());
                Assert.Equal(4, session.QueryIndex<ArticlesByDay>().Count());
            }

            // delete a document
            using (var session = _store.CreateSession())
            {
                var article = session.QueryByReducedIndex<ArticlesByDay, Article>(
                        a => a.Where(b => b.DayOfYear == new DateTime(2011, 11, 2).DayOfYear)
                    ).FirstOrDefault();
                
                Assert.NotNull(article);

                article.PublishedUtc = new DateTime(2011, 11, 3);

                session.Save(article);

                session.Commit();
            }

            // there should be the same number of indexes
            using (var session = _store.CreateSession())
            {
                Assert.Equal(10, session.QueryDocument<Article>().Count());
                Assert.Equal(4, session.QueryIndex<ArticlesByDay>().Count());

                Assert.Equal(4,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).Count);
                Assert.Equal(2,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).Count);
                Assert.Equal(3,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).Count);
                Assert.Equal(1,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).Count);
            }
        }

        [Fact]
        public void AlteringDocumentShouldUpdateReducedIndex()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var dates = new[]
                {
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 3),
                    new DateTime(2011, 11, 4),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 3),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 1)
                };

                var articles = dates.Select(x => new Article
                {
                    PublishedUtc = x
                });

                foreach (var article in articles)
                {
                    session.Save(article);
                }

                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(10, session.QueryDocument<Article>().Count());
                Assert.Equal(4, session.QueryIndex<ArticlesByDay>().Count());
            }

            // update a document
            using (var session = _store.CreateSession())
            {
                var article =
                    session.QueryByReducedIndex<ArticlesByDay, Article>(
                        a => a.Where(b => b.DayOfYear == new DateTime(2011, 11, 4).DayOfYear)).FirstOrDefault();
                Assert.NotNull(article);
                session.Delete(article);

                session.Commit();
            }

            // there should be only 3 indexes left
            using (var session = _store.CreateSession())
            {
                // document was not deleted
                Assert.Equal(9, session.QueryDocument<Article>().Count());
                // index was not deleted
                Assert.Equal(3, session.QueryIndex<ArticlesByDay>().Count());
            }
        }

        [Fact]
        public void IndexHasLinkToDocuments()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var d1 = new Article {PublishedUtc = new DateTime(2011, 11, 1)};
                var d2 = new Article {PublishedUtc = new DateTime(2011, 11, 1)};

                session.Save(d1);
                session.Save(d2);

                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                var articles = session.QueryDocument<Article>();
                Assert.Equal(2, articles.Count());
            }
        }

        [Fact]
        public void ShouldSaveCustomObjectAsync()
        {
            var session = _store.CreateSession();
                var bill = new Person {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

            session.Save(bill);
            var task = session.CommitAsync();

            task.Wait();
        }

        [Fact]
        public void ShouldPageResults() {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession()) {
                for (int i = 0; i < 100; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + i,
                        Lastname = "Gates" + i,
                    };

                    session.Save(person);
                }

                session.Commit();
            }

            using (var session = _store.CreateSession()) {
                Assert.Equal(100, session.QueryIndex<PersonByName>().Count());
                Assert.Equal(10, session.QueryIndex<PersonByName>().OrderBy(x => x.Name).Skip(0).Take(10).ToList().Count);
                Assert.Equal(1, session.QueryIndex<PersonByName>().Count(x => x.Name == "Bill0"));

                var persons = session.QueryByMappedIndex<PersonByName, Person>(
                    q => q.Take(10)
                );

                Assert.Equal(10, persons.Count());
            }
        }
    }
}
