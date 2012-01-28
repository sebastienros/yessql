using System;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YesSql.Core.Data;
using YesSql.Core.Data.Models;
using YesSql.Core.Services;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    [TestClass]
    public class CoreTests
    {
        private IStore _store;

        [TestInitialize]
        public void Init()
        {

            // delete the db before starting tests
            if (File.Exists("Store.sdf"))
            {
                File.Delete("Store.sdf");
            }

            // recreating a fresh SqlCe db
            new SqlCeEngine {LocalConnectionString = "Data Source=Store.sdf"}.CreateDatabase();

            // using app.config settings as fluent config doesn't provide MsSqlCe40Dialect as a choice
            // () => MsSqlCeConfiguration.Standard.ConnectionString("Data Source=Store.sdf")

            _store = new Store().Configure(MsSqlCeConfiguration.MsSqlCe40.ConnectionString("Data Source=Store.sdf").ShowSql());

        }

        [TestCleanup]
        public void Cleanup()
        {
            _store.Dispose();
        }

        [TestMethod]
        public void ShouldCreateDatabase()
        {

            using (var session = _store.CreateSession())
            {
                var doc = new Document {Type = "Product", Content = "{}"};

                session.Save(doc);
                session.Commit();
            }
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

                Assert.IsNotNull(person);
                Assert.AreEqual("Bill", person.Firstname);
                Assert.AreEqual("Gates", person.Lastname);

                Assert.IsNotNull(person.Address);
                Assert.AreEqual("1 Microsoft Way", person.Address.Street);
                Assert.AreEqual("Redmond", person.Address.City);
            }
        }

        [TestMethod]
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
                Assert.IsNotNull(order);
                Assert.AreEqual(1, order.OrderLines.Count);

                var prod =
                    session.QueryDocument<Product>(q => q.FirstOrDefault(x => x.Id == order.OrderLines[0].ProductId));
                Assert.IsNotNull(prod);
                Assert.AreEqual("Milk", prod.Name);
            }
        }

        [TestMethod]
        public void ShouldCreateIndexAndLinkToDocument()
        {
            _store.RegisterIndexes<PersonByName>();

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
                Assert.AreEqual(2, session.QueryIndex<PersonByName>().Count());
                Assert.AreEqual(1, session.QueryIndex<PersonByName>().Count(x => x.Name == "Bill"));
                Assert.AreEqual(1, session.QueryIndex<PersonByName>().Count(x => x.Name == "Steve"));
                Assert.AreEqual(0, session.QueryIndex<PersonByName>().Count(x => x.Name == "Joe"));

                var person =
                    session.QueryByMappedIndex<PersonByName, Person>(q => q.FirstOrDefault(x => x.Name == "Bill"));
                Assert.IsNotNull(person);
                Assert.AreEqual("Bill", person.Firstname);
            }
        }

        [TestMethod]
        public void ShouldReduce()
        {
            _store.RegisterIndexes<ArticleByDayIndexer>();

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
                Assert.AreEqual(4, session.QueryIndex<ArticlesByDay>().Count());

                Assert.AreEqual(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear));
                Assert.AreEqual(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear));
                Assert.AreEqual(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear));
                Assert.AreEqual(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear));

                Assert.AreEqual(4,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).Count);
                Assert.AreEqual(3,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).Count);
                Assert.AreEqual(2,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).Count);
                Assert.AreEqual(1,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).Count);
            }
        }

        [TestMethod]
        public void ShouldReduceAndMergeWithDatabase()
        {
            _store.RegisterIndexes(typeof (ArticleByDayIndexer).Assembly);

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
                Assert.AreEqual(4, session.QueryIndex<ArticlesByDay>().Count());

                Assert.AreEqual(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear));
                Assert.AreEqual(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear));
                Assert.AreEqual(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear));
                Assert.AreEqual(1,
                                session.QueryIndex<ArticlesByDay>().Count(
                                    x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear));

                Assert.AreEqual(5,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).Count);
                Assert.AreEqual(3,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).Count);
                Assert.AreEqual(2,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).Count);
                Assert.AreEqual(1,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).Count);
            }
        }

        [TestMethod]
        public void ShouldDeleteCustomObject()
        {
            _store.RegisterIndexes<PersonByName>();

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
                Assert.IsNotNull(person);

                session.Delete(person);
                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                var person = session.QueryDocument<Person>(q => q.FirstOrDefault());
                Assert.IsNull(person);
            }
        }

        [TestMethod]
        public void RemovingDocumentShouldDeleteMappedIndex()
        {
            _store.RegisterIndexes<PersonByName>();

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
                Assert.IsNotNull(personByName);

                var person = session.QueryDocument<Person>(q => q.FirstOrDefault());
                Assert.IsNotNull(person);

                session.Delete(person);
                session.Commit();
            }

            using (var session = _store.CreateSession())
            {
                var personByName = session.QueryIndex<PersonByName>().FirstOrDefault();
                Assert.IsNull(personByName);
            }
        }

        [TestMethod]
        public void RemovingDocumentShouldDeleteReducedIndex()
        {
            _store.RegisterIndexes<ArticleByDayIndexer>();

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
                Assert.AreEqual(10, session.QueryDocument<Article>().Count());
                Assert.AreEqual(4, session.QueryIndex<ArticlesByDay>().Count());
            }

            // delete a document
            using (var session = _store.CreateSession())
            {
                var article =
                    session.QueryByReducedIndex<ArticlesByDay, Article>(
                        a => a.Where(b => b.DayOfYear == new DateTime(2011, 11, 4).DayOfYear)).FirstOrDefault();
                Assert.IsNotNull(article);
                session.Delete(article);

                session.Commit();
            }

            // there should be only 3 indexes left
            using (var session = _store.CreateSession())
            {
                Assert.AreEqual(9, session.QueryDocument<Article>().Count(), "Document was not deleted");
                Assert.AreEqual(3, session.QueryIndex<ArticlesByDay>().Count(), "Index was not deleted");
            }
        }

        [TestMethod]
        public void UpdatingDocumentShouldUpdateReducedIndex()
        {
            _store.RegisterIndexes<ArticleByDayIndexer>();

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
                Assert.AreEqual(10, session.QueryDocument<Article>().Count());
                Assert.AreEqual(4, session.QueryIndex<ArticlesByDay>().Count());
            }

            // delete a document
            using (var session = _store.CreateSession())
            {
                var article =
                    session.QueryByReducedIndex<ArticlesByDay, Article>(
                        a => a.Where(b => b.DayOfYear == new DateTime(2011, 11, 2).DayOfYear)).FirstOrDefault();
                Assert.IsNotNull(article);

                article.PublishedUtc = new DateTime(2011, 11, 3);

                session.Save(article);

                session.Commit();
            }

            // there should be the same number of indexes
            using (var session = _store.CreateSession())
            {
                Assert.AreEqual(10, session.QueryDocument<Article>().Count());
                Assert.AreEqual(4, session.QueryIndex<ArticlesByDay>().Count());

                Assert.AreEqual(4,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).Count);
                Assert.AreEqual(2,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).Count);
                Assert.AreEqual(3,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).Count);
                Assert.AreEqual(1,
                                session.QueryIndex<ArticlesByDay>().Single(
                                    x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).Count);
            }
        }

        [TestMethod]
        public void AlteringDocumentShouldUpdateReducedIndex()
        {
            _store.RegisterIndexes<ArticleByDayIndexer>();

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
                Assert.AreEqual(10, session.QueryDocument<Article>().Count());
                Assert.AreEqual(4, session.QueryIndex<ArticlesByDay>().Count());
            }

            // update a document
            using (var session = _store.CreateSession())
            {
                var article =
                    session.QueryByReducedIndex<ArticlesByDay, Article>(
                        a => a.Where(b => b.DayOfYear == new DateTime(2011, 11, 4).DayOfYear)).FirstOrDefault();
                Assert.IsNotNull(article);
                session.Delete(article);

                session.Commit();
            }

            // there should be only 3 indexes left
            using (var session = _store.CreateSession())
            {
                Assert.AreEqual(9, session.QueryDocument<Article>().Count(), "Document was not deleted");
                Assert.AreEqual(3, session.QueryIndex<ArticlesByDay>().Count(), "Index was not deleted");
            }
        }

        [TestMethod]
        public void IndexHasLinkToDocuments()
        {
            _store.RegisterIndexes<ArticleByDayIndexer>();

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
                Assert.AreEqual(2, articles.Count());
            }
        }

        [TestMethod]
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

        [TestMethod]
        public void ShouldPageResults() {
            _store.RegisterIndexes<PersonByName>();

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
                Assert.AreEqual(100, session.QueryIndex<PersonByName>().Count());
                Assert.AreEqual(10, session.QueryIndex<PersonByName>().OrderBy(x => x.Name).Skip(0).Take(10).ToList().Count);
                Assert.AreEqual(1, session.QueryIndex<PersonByName>().Count(x => x.Name == "Bill0"));

                var persons = session.QueryByMappedIndex<PersonByName, Person>(
                    q => q.Take(10)
                );

                Assert.AreEqual(10, persons.Count());
            }
        }
    }
}
