using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YesSql.Collections;
using YesSql.Services;
using YesSql.Sql;
using YesSql.Tests.CompiledQueries;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    public abstract class CoreTests : IDisposable
    {
        protected virtual string TablePrefix => "tp";

        protected IStore _store;

        public CoreTests()
        {

        }

        public void Dispose()
        {
            CleanDatabase(false);
            _store.Dispose();

            OnDispose();
        }

        protected virtual void OnDispose()
        {
        }

        //[DebuggerNonUserCode]
        protected virtual void CleanDatabase(bool throwOnError)
        {
            // Remove existing tables
            using (var session = _store.CreateSession())
            {
                var builder = new SchemaBuilder(session) { ThrowOnError = throwOnError };

                builder.DropReduceIndexTable(nameof(ArticlesByDay));
                builder.DropReduceIndexTable(nameof(AttachmentByDay));
                builder.DropMapIndexTable(nameof(ArticleByPublishedDate));
                builder.DropMapIndexTable(nameof(PersonByName));
                builder.DropMapIndexTable(nameof(PersonIdentity));

                using (new NamedCollection("Collection1"))
                {
                    builder.DropMapIndexTable(nameof(PersonByNameCol));
                }

                builder.DropMapIndexTable(nameof(PersonByAge));
                builder.DropMapIndexTable(nameof(PublishedArticle));
                builder.DropReduceIndexTable(nameof(UserByRoleNameIndex));
                builder.DropTable(Store.DocumentTable);
                builder.DropTable("Collection1_Document");
                builder.DropTable(LinearBlockIdGenerator.TableName);

                OnCleanDatabase(builder, session);
            }
        }

        protected virtual void OnCleanDatabase(SchemaBuilder builder, ISession session)
        {

        }

        public void CreateTables()
        {
            // Create tables
            _store.InitializeAsync().Wait();

            using (var session = _store.CreateSession())
            {
                var builder = new SchemaBuilder(session);

                builder.CreateReduceIndexTable(nameof(ArticlesByDay), column => column
                        .Column<int>(nameof(ArticlesByDay.Count))
                        .Column<int>(nameof(ArticlesByDay.DayOfYear))
                    );
                builder.CreateReduceIndexTable(nameof(AttachmentByDay), column => column
                        .Column<int>(nameof(AttachmentByDay.Count))
                        .Column<int>(nameof(AttachmentByDay.Date))
                    );

                builder.CreateReduceIndexTable(nameof(UserByRoleNameIndex), column => column
                        .Column<int>(nameof(UserByRoleNameIndex.Count))
                        .Column<string>(nameof(UserByRoleNameIndex.RoleName))
                    );

                builder.CreateMapIndexTable(nameof(ArticleByPublishedDate), column => column
                        .Column<DateTime>(nameof(ArticleByPublishedDate.PublishedDateTime))
                        .Column<string>(nameof(ArticleByPublishedDate.Title))
                    );

                builder.CreateMapIndexTable(nameof(PersonByName), column => column
                        .Column<string>(nameof(PersonByName.SomeName))
                    );

                builder.CreateMapIndexTable(nameof(PersonIdentity), column => column
                        .Column<string>(nameof(PersonIdentity.Identity))
                    );

                builder.CreateMapIndexTable(nameof(PersonByAge), column => column
                        .Column<int>(nameof(PersonByAge.Age))
                        .Column<bool>(nameof(PersonByAge.Adult))
                        .Column<string>(nameof(PersonByAge.Name))
                    );

                builder.CreateMapIndexTable(nameof(PublishedArticle), column => { });
            }
        }

        [Fact]
        public void ShouldCreateDatabase()
        {
            using (var session = _store.CreateSession())
            {
                var doc = new Product();
                session.Save(doc);
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
            }
        }

        [Fact]
        public async Task ShouldLoadAnonymousDocument()
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
            }

            using (var session = _store.CreateSession())
            {
                dynamic person = await session.Query().Any().FirstOrDefaultAsync();

                Assert.NotNull(person);
                Assert.Equal("Bill", (string)person.Firstname);
                Assert.Equal("Gates", (string)person.Lastname);

                Assert.NotNull(person.Address);
                Assert.Equal("1 Microsoft Way", (string)person.Address.Street);
                Assert.Equal("Redmond", (string)person.Address.City);
            }
        }


        [Fact]
        public async Task ShouldQueryNonExistentResult()
        {
            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.Null(person);
            }
        }

        [Fact]
        public async Task ShouldUpdateNewDocument()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person { Firstname = "Bill" };

                session.Save(bill);

                bill.Lastname = "Gates";

                session.Save(bill);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.Query<Person>().CountAsync());

                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.Equal("Bill", person.Firstname);
                Assert.Equal("Gates", person.Lastname);
            }
        }

        [Fact]
        public async Task ShouldQueryIndexWithParameter()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person { Firstname = "Bill" };
                session.Save(bill);
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.QueryIndex<PersonByName>().Where(d => d.QuoteForColumnName(nameof(PersonByName.SomeName)) + " = @Name").WithParameter("Name", "Bill").FirstOrDefaultAsync();

                Assert.NotNull(person);
                Assert.Equal("Bill", (string)person.SomeName);
            }
        }

        [Fact]
        public async Task ShouldMapAsyncIndex()
        {
            _store.RegisterIndexes<PersonAsyncIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person { Firstname = "Bill" };
                session.Save(bill);
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.QueryIndex<PersonByName>().FirstOrDefaultAsync();

                Assert.NotNull(person);
                Assert.Equal("Bill", person.SomeName);
            }
        }

        [Fact]
        public async Task ShouldResolveScopedIndexProviders()
        {

            using (var session = _store.CreateSession())
            {
                session.RegisterIndexes(new ScopedPersonAsyncIndexProvider(1));

                session.Save(new Person { Firstname = "Bill" });
            }

            using (var session = _store.CreateSession())
            {
                session.RegisterIndexes(new ScopedPersonAsyncIndexProvider(2));

                session.Save(new Person { Firstname = "Bill" });
                session.Save(new Person { Firstname = "Steve" });
            }

            using (var session = _store.CreateSession())
            {
                var count = await session.QueryIndex<PersonByName>().CountAsync();
                var bill1 = await session.QueryIndex<PersonByName>(x => x.SomeName == "Bill1").FirstOrDefaultAsync();
                var bill2 = await session.QueryIndex<PersonByName>(x => x.SomeName == "Bill2").FirstOrDefaultAsync();
                var steve2 = await session.QueryIndex<PersonByName>(x => x.SomeName == "Steve2").FirstOrDefaultAsync();

                Assert.Equal(3, count);
                Assert.NotNull(bill1);
                Assert.NotNull(bill2);
                Assert.NotNull(steve2);
            }
        }

        [Fact]
        public async Task ShouldQueryNullValues()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Person { Firstname = null });
                session.Save(new Person { Firstname = "a" });
                session.Save(new Person { Firstname = "b" });
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<PersonByName>(x => x.SomeName == null).CountAsync());
                Assert.Equal(2, await session.QueryIndex<PersonByName>(x => x.SomeName != null).CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>(x => null == x.SomeName).CountAsync());
                Assert.Equal(2, await session.QueryIndex<PersonByName>(x => null != x.SomeName).CountAsync());
                Assert.Equal(3, await session.QueryIndex<PersonByName>(x => null == null).CountAsync());
                Assert.Equal(0, await session.QueryIndex<PersonByName>(x => null != null).CountAsync());
            }
        }

        [Fact]
        public async Task ShouldCompareWithConstants()
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Article { Title = TestConstants.Strings.SomeString, PublishedUtc = new DateTime(2011, 11, 1) });
                session.Save(new Article { Title = TestConstants.Strings.SomeOtherString, PublishedUtc = new DateTime(2011, 11, 1) });
                session.Save(new Article { Title = TestConstants.Strings.SomeString, PublishedUtc = new DateTime(2011, 11, 2) });
                session.Save(new Article { Title = TestConstants.Strings.SomeOtherString, PublishedUtc = new DateTime(2011, 11, 2) });
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query<Article, ArticleByPublishedDate>(x => x.Title == TestConstants.Strings.SomeString).CountAsync());
                Assert.Equal(2, await session.Query<Article, ArticleByPublishedDate>(x => x.Title != TestConstants.Strings.SomeString).CountAsync());
                Assert.Equal(4, await session.Query<Article, ArticleByPublishedDate>(x => x.PublishedDateTime < DateTime.UtcNow).CountAsync());
                Assert.Equal(2, await session.Query<Article, ArticleByPublishedDate>(x => x.Title != TestConstants.Strings.SomeString && x.PublishedDateTime < DateTime.UtcNow).CountAsync());
            }
        }

        [Fact]
        public async Task ShouldQueryDocumentWithParameter()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person { Firstname = "Bill" };
                session.Save(bill);
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person, PersonByName>().Where(d => d.QuoteForColumnName(nameof(PersonByName.SomeName)) + " = @Name").WithParameter("Name", "Bill").FirstOrDefaultAsync();

                Assert.NotNull(person);
                Assert.Equal("Bill", (string)person.Firstname);
            }
        }

        [Fact]
        public async Task ShouldQuoteString()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person { Firstname = "Bill" };
                var steve = new Person { Firstname = "Steve" };
                session.Save(bill);
                session.Save(steve);
            }

            using (var session = _store.CreateSession())
            {
                var connection = (await session.DemandAsync()).Connection;
                var dialect = SqlDialectFactory.For(connection);
                var sql = dialect.QuoteForColumnName(nameof(PersonByName.SomeName)) + " = " + dialect.GetSqlValue("Bill");

                var person = await session.Query<Person, PersonByName>().Where(sql).FirstOrDefaultAsync();

                Assert.NotNull(person);
                Assert.Equal("Bill", (string)person.Firstname);
            }
        }

        [Fact]
        public async Task ShouldSerializeComplexObject()
        {
            int productId;

            using (var session = _store.CreateSession())
            {
                var product = new Product
                {
                    Cost = 3.99m,
                    Name = "Milk",
                };

                session.Save(product);
                await session.CommitAsync();
                productId = product.Id;

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
            }

            using (var session = _store.CreateSession())
            {
                var prod = await session.GetAsync<Product>(productId);
                Assert.NotNull(prod);
                Assert.Equal("Milk", prod.Name);
            }
        }

        [Fact]
        public void ShouldAssignIdWhenSaved()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                Assert.True(bill.Id == 0);
                session.Save(bill);
                Assert.True(bill.Id != 0);
            }
        }

        [Fact]
        public async Task ShouldAutoFlushOnGet()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                session.Save(bill);
                var newBill = await session.GetAsync<Person>(bill.Id);

                Assert.Same(newBill, bill);
            }
        }

        [Fact]
        public async Task ShouldKeepTrackedOnAutoFlush()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                session.Save(bill);
                var newBill = await session.GetAsync<Person>(bill.Id);

                Assert.Same(newBill, bill);
            }
        }

        [Fact]
        public async Task ShouldUpdateAutoflushedIndex()
        {
            // When auto-flush is called on an entity
            // its indexes should be updated on the actual commit

            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                session.Save(bill);

                // This query triggers an auto-flush

                Assert.Equal(1, await session.Query<Person, PersonByName>().CountAsync());

                bill.Firstname = "Bill2";
                session.Save(bill);

                Assert.Equal(1, await session.Query<Person, PersonByName>().Where(x => x.SomeName == "Bill2").CountAsync());

                bill.Firstname = "Bill3";
                session.Save(bill);

                Assert.Equal(1, await session.QueryIndex<PersonByName>().CountAsync());
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<PersonByName>().CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>().Where(x => x.SomeName == "Bill3").CountAsync());
            }
        }

        [Fact]
        public async Task ShouldQueryBoolean()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Age = 50
                };

                var elon = new Person
                {
                    Firstname = "Elon",
                    Lastname = "Musk",
                    Age = 12
                };

                session.Save(bill);
                session.Save(elon);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<PersonByAge>(x => x.Adult && x.Adult).CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByAge>(x => x.Adult).CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByAge>(x => x.Adult == true).CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByAge>(x => !x.Adult).CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByAge>(x => x.Adult == false).CountAsync());

                var firstname = "Bill";
                Assert.NotNull(await session.Query<Person, PersonByAge>().Where(x => x.Name == "Bill" && x.Adult == true).FirstOrDefaultAsync());
                Assert.NotNull(await session.Query<Person, PersonByAge>().Where(x => x.Name == firstname && x.Adult == true).FirstOrDefaultAsync());

                // bool && IsIn
                Assert.Null(await session.Query<Person, PersonByAge>().Where(x => x.Adult && x.Name.IsIn(new string[0])).FirstOrDefaultAsync());
                Assert.NotNull(await session.Query<Person, PersonByAge>().Where(x => x.Adult && x.Name.IsIn(new[] { "Bill" })).FirstOrDefaultAsync());
                Assert.NotNull(await session.Query<Person, PersonByAge>().Where(x => x.Adult && x.Name.IsIn(new[] { "Bill", "Steve" })).FirstOrDefaultAsync());

                Assert.NotNull(await session.Query<Person, PersonByAge>().Where(x => x.Age.IsIn(new[] { 12 })).FirstOrDefaultAsync());
                Assert.Null(await session.Query<Person, PersonByAge>().Where(x => x.Age.IsIn(new[] { 1, 2, 3 }.Cast<object>())).FirstOrDefaultAsync());
            }
        }

        [Fact]
        public async Task ShouldQueryWithCompiledQueries()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Age = 50
                };

                var elon = new Person
                {
                    Firstname = "Elon",
                    Lastname = "Musk",
                    Age = 12
                };

                var eilon = new Person
                {
                    Firstname = "Eilon",
                    Lastname = "Lipton",
                    Age = 12
                };

                session.Save(bill);
                session.Save(elon);
                session.Save(eilon);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.ExecuteQuery(new PersonByNameOrAgeQuery(50, null)).CountAsync());
                Assert.Equal(2, await session.ExecuteQuery(new PersonByNameOrAgeQuery(12, null)).CountAsync());
                Assert.Equal(0, await session.ExecuteQuery(new PersonByNameOrAgeQuery(10, null)).CountAsync());

                Assert.Equal(3, await session.ExecuteQuery(new PersonByNameOrAgeQuery(12, "Bill")).CountAsync());
                Assert.Equal(2, await session.ExecuteQuery(new PersonByNameOrAgeQuery(50, "Elon")).CountAsync());
                Assert.Equal(2, await session.ExecuteQuery(new PersonByNameOrAgeQuery(50, "Eilon")).CountAsync());
                Assert.Equal(0, await session.ExecuteQuery(new PersonByNameOrAgeQuery(10, "Mark")).CountAsync());

                Assert.Single(await session.ExecuteQuery(new PersonByNameOrAgeQuery(50, null)).ListAsync());
                Assert.Single(await session.ExecuteQuery(new PersonByNameOrAgeQuery(50, null)).ListAsync());
                Assert.Empty(await session.ExecuteQuery(new PersonByNameOrAgeQuery(10, null)).ListAsync());

                Assert.Equal("Bill", (await session.ExecuteQuery(new PersonByNameOrAgeQuery(50, null)).FirstOrDefaultAsync()).Firstname);
            }
        }

        [Fact]
        public async Task ShouldOrderCompiledQueries()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Age = 50
                };

                var elon = new Person
                {
                    Firstname = "Elon",
                    Lastname = "Musk",
                    Age = 12
                };

                session.Save(bill);
                session.Save(elon);
            }

            using (var session = _store.CreateSession())
            {
                var results = await session.ExecuteQuery(new PersonOrderedAscQuery()).ListAsync();

                Assert.Equal("Elon", results.ElementAt(0).Firstname);
                Assert.Equal("Bill", results.ElementAt(1).Firstname);
            }

            using (var session = _store.CreateSession())
            {
                var results = await session.ExecuteQuery(new PersonOrderedDescQuery()).ListAsync();

                Assert.Equal("Bill", results.ElementAt(0).Firstname);
                Assert.Equal("Elon", results.ElementAt(1).Firstname);
            }
        }

        [Fact]
        public virtual async Task ShouldRunCompiledQueriesConcurrently()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();

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
            }

            var concurrency = 20;
            var MaxTransactions = 10000;

            var counter = 0;
            var stopping = false;

            var tasks = Enumerable.Range(1, concurrency).Select(i => Task.Run(async () =>
            {
                while (!stopping && Interlocked.Add(ref counter, 1) < MaxTransactions)
                {
                    using (var session = _store.CreateSession())
                    {
                        Assert.Equal(2, await session.ExecuteQuery(new PersonByNameOrAgeQuery(0, "Bill")).CountAsync());
                    }
                }
            })).ToList();

            tasks.Add(Task.Delay(TimeSpan.FromSeconds(5)));

            await Task.WhenAny(tasks);

            // Flushing tasks
            stopping = true;
            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task ShouldNotLeakPagingBetweenQueries()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Age = 50
                };

                var elon = new Person
                {
                    Firstname = "Elon",
                    Lastname = "Musk",
                    Age = 12
                };

                var eilon = new Person
                {
                    Firstname = "Eilon",
                    Lastname = "Lipton",
                    Age = 12
                };

                session.Save(bill);
                session.Save(elon);
                session.Save(eilon);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(12, (await session.ExecuteQuery(new PersonByNameOrAgeQuery(12, null)).FirstOrDefaultAsync()).Age);
                Assert.Equal(2, (await session.ExecuteQuery(new PersonByNameOrAgeQuery(12, null)).ListAsync()).Count());
            }
        }

        [Fact]
        public async Task ShouldQueryInnerSelect()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Age = 50
                };

                var elon = new Person
                {
                    Firstname = "Elon",
                    Lastname = "Musk",
                    Age = 12
                };

                session.Save(bill);
                session.Save(elon);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.Query<Person, PersonByAge>().Where(x => x.Name.IsIn<PersonByName>(y => y.SomeName, y => y.SomeName.StartsWith("B") || y.SomeName.StartsWith("C"))).CountAsync());
                Assert.Equal(2, await session.Query<Person, PersonByAge>().Where(x => x.Name.IsIn<PersonByName>(y => y.SomeName, y => y.SomeName.StartsWith("B") || y.SomeName.Contains("lo"))).CountAsync());

                Assert.Equal(1, await session.Query<Person, PersonByAge>().Where(x => x.Name.IsNotIn<PersonByName>(y => y.SomeName, y => y.SomeName.StartsWith("B") || y.SomeName.StartsWith("C"))).CountAsync());
                Assert.Equal(0, await session.Query<Person, PersonByAge>().Where(x => x.Name.IsNotIn<PersonByName>(y => y.SomeName, y => y.SomeName.StartsWith("B") || y.SomeName.Contains("lo"))).CountAsync());
            }
        }

        [Fact]
        public async Task ShouldConcatenateMembers()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Age = 50
                };

                var elon = new Person
                {
                    Firstname = "Elon",
                    Lastname = "Musk",
                    Age = 12
                };

                session.Save(bill);
                session.Save(elon);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.Query<Person, PersonByAge>().Where(x => x.Name + x.Name == "BillBill").CountAsync());
                Assert.Equal(1, await session.Query<Person, PersonByAge>().Where(x => x.Name + " " + x.Name == "Bill Bill").CountAsync());
                Assert.Equal(0, await session.Query<Person, PersonByAge>().Where(x => x.Name + " " + x.Name == "Foo").CountAsync());
                Assert.Equal(1, await session.Query<Person, PersonByAge>().Where(x => (x.Name + x.Name).Contains("Bill")).CountAsync());

                // Concat method
                Assert.Equal(1, await session.Query<Person, PersonByAge>().Where(x => String.Concat(x.Name, x.Name) == "BillBill").CountAsync());
                Assert.Equal(1, await session.Query<Person, PersonByAge>().Where(x => String.Concat(x.Name, x.Name, x.Name) == "BillBillBill").CountAsync());
                Assert.Equal(1, await session.Query<Person, PersonByAge>().Where(x => String.Concat(x.Name, x.Name, x.Name, x.Name) == "BillBillBillBill").CountAsync());
            }
        }

        [Fact]
        public virtual async Task ShouldAppendIndexOnUpdate()
        {
            // When an object is updated, its map indexes
            // should be created a new records (to support append only scenarios)

            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                session.Save(bill);

                var p1 = await session.QueryIndex<PersonByName>().FirstOrDefaultAsync();

                Assert.Equal(1, p1.Id);

                bill.Firstname = "Bill2";
                session.Save(bill);

                var p2 = await session.QueryIndex<PersonByName>().FirstOrDefaultAsync();

                Assert.Equal(2, p2.Id);

                bill.Firstname = "Bill3";
                session.Save(bill);

                var p3 = await session.QueryIndex<PersonByName>().FirstOrDefaultAsync();

                Assert.Equal(3, p3.Id);

            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<PersonByName>().CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>().Where(x => x.SomeName == "Bill3").CountAsync());
            }
        }

        [Fact]
        public async Task ShouldCreateSeveralMapIndexPerDocument()
        {
            // When an index returns multiple map indexes, they should all be stored
            // and queryable.

            // This test also ensure we can use a SQL keyword as the column name (Identity)

            _store.RegisterIndexes<PersonIdentitiesIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var hanselman = new Person
                {
                    Firstname = "Scott",
                    Lastname = "Hanselman"
                };

                var guthrie = new Person
                {
                    Firstname = "Scott",
                    Lastname = "Guthrie"
                };

                session.Save(hanselman);
                session.Save(guthrie);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(4, await session.QueryIndex<PersonIdentity>().CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonIdentity>().Where(x => x.Identity == "Hanselman").CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonIdentity>().Where(x => x.Identity == "Guthrie").CountAsync());
                Assert.Equal(2, await session.QueryIndex<PersonIdentity>().Where(x => x.Identity == "Scott").CountAsync());
            }
        }

        [Fact]
        public async Task ShouldDeletePreviousIndexes()
        {
            // When an index returns multiple map indexes, changing these results should remove the previous ones.

            _store.RegisterIndexes<PersonIdentitiesIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var guthrie = new Person
                {
                    Firstname = "Scott",
                    Lastname = "Guthrie"
                };

                session.Save(guthrie);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.QueryIndex<PersonIdentity>().CountAsync());
            }

            using (var session = _store.CreateSession())
            {
                var guthrie = await session.Query<Person, PersonIdentity>(x => x.Identity == "Scott").FirstOrDefaultAsync();
                guthrie.Lastname = "Gu";

                session.Save(guthrie);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.QueryIndex<PersonIdentity>().CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonIdentity>().Where(x => x.Identity == "Scott").CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonIdentity>().Where(x => x.Identity == "Gu").CountAsync());
            }

            using (var session = _store.CreateSession())
            {
                var guthrie = await session.Query<Person, PersonIdentity>(x => x.Identity == "Scott").FirstOrDefaultAsync();
                guthrie.Anonymous = true;

                session.Save(guthrie);

                Assert.Equal(0, await session.QueryIndex<PersonIdentity>().CountAsync());
            }
        }

        [Fact]
        public async Task ShouldCreateIndexAndLinkToDocument()
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
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.QueryIndex<PersonByName>().CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>(x => x.SomeName == "Bill").CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>(x => x.SomeName == "Steve").CountAsync());
                Assert.Equal(0, await session.QueryIndex<PersonByName>(x => x.SomeName == "Joe").CountAsync());

                var person = await session
                    .Query<Person, PersonByName>()
                    .Where(x => x.SomeName == "Bill")
                    .FirstOrDefaultAsync();

                Assert.NotNull(person);
                Assert.Equal("Bill", person.Firstname);
            }
        }

        [Fact]
        public async Task ShouldJoinMapIndexes()
        {
            _store.RegisterIndexes<PersonIndexProvider>();
            _store.RegisterIndexes<PersonAgeIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Age = 1
                };

                var steve = new Person
                {
                    Firstname = "Steve",
                    Age = 2
                };

                var paul = new Person
                {
                    Firstname = "Paul",
                    Age = 2
                };

                session.Save(bill);
                session.Save(steve);
                session.Save(paul);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(3, await session.QueryIndex<PersonByName>().CountAsync());
                Assert.Equal(2, await session.QueryIndex<PersonByAge>(x => x.Age == 2).CountAsync());
                Assert.Equal(1, await session.Query().For<Person>()
                    .With<PersonByName>(x => x.SomeName == "Steve")
                    .With<PersonByAge>(x => x.Age == 2)
                    .CountAsync());
            }
        }

        [Fact]
        public async Task ShouldReturnImplicitlyFilteredType()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Article());
                session.Save(new Person { Firstname = "Bill" });
                session.Save(new Person { Firstname = "Steve" });
                session.Save(new Person { Firstname = "Paul" });
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(3, await session.QueryIndex<PersonByName>().CountAsync());
                Assert.Equal(3, await session.Query().For<Person>().CountAsync());
                Assert.Equal(3, await session.Query().For<Person>(false).With<PersonByName>().CountAsync());
                Assert.Equal(4, await session.Query().For<Person>(false).CountAsync());
            }
        }

        [Fact]
        public async Task ShouldOrderJoinedMapIndexes()
        {
            _store.RegisterIndexes<PersonIndexProvider>();
            _store.RegisterIndexes<PersonAgeIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Age = 1
                };

                var steve = new Person
                {
                    Firstname = "Steve",
                    Age = 2
                };

                var paul = new Person
                {
                    Firstname = "Scott",
                    Age = 2
                };

                session.Save(bill);
                session.Save(steve);
                session.Save(paul);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query().For<Person>()
                    .With<PersonByName>(x => x.SomeName.StartsWith("S"))
                    .With<PersonByAge>(x => x.Age == 2)
                    .CountAsync());

                Assert.Equal("Scott", (await session.Query().For<Person>()
                    .With<PersonByName>(x => x.SomeName.StartsWith("S"))
                    .OrderBy(x => x.SomeName)
                    .With<PersonByAge>(x => x.Age == 2)
                    .FirstOrDefaultAsync())
                    .Firstname);

                Assert.Equal("Steve", (await session.Query().For<Person>()
                    .With<PersonByName>(x => x.SomeName.StartsWith("S"))
                    .OrderByDescending(x => x.SomeName)
                    .With<PersonByAge>(x => x.Age == 2)
                    .FirstOrDefaultAsync())
                    .Firstname);
            }
        }

        [Fact]
        public async Task ShouldJoinReduceIndex()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();
            _store.RegisterIndexes<PublishedArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var dates = new[]
                {
                    new DateTime(2011, 11, 1), // published
                    new DateTime(2011, 11, 2), // not published
                    new DateTime(2011, 11, 3), // published
                    new DateTime(2011, 11, 4), // not published
                    new DateTime(2011, 11, 1), // published
                    new DateTime(2011, 11, 2), // not published
                    new DateTime(2011, 11, 3), // published
                    new DateTime(2011, 11, 1), // not published
                    new DateTime(2011, 11, 2), // published
                    new DateTime(2011, 11, 1) // not published
                };

                var articles = dates.Select((x, i) => new Article
                {
                    IsPublished = i % 2 == 0, // half are published
                    PublishedUtc = x
                });

                foreach (var article in articles)
                {
                    session.Save(article);
                }

            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(4, await session.QueryIndex<ArticlesByDay>().CountAsync());

                Assert.Equal(4, await session.Query().For<Article>().With<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).CountAsync());
                Assert.Equal(3, await session.Query().For<Article>().With<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).CountAsync());
                Assert.Equal(2, await session.Query().For<Article>().With<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).CountAsync());
                Assert.Equal(1, await session.Query().For<Article>().With<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).CountAsync());

                Assert.Equal(2, await session.Query().For<Article>().With<PublishedArticle>().With<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).CountAsync());
                Assert.Equal(1, await session.Query().For<Article>().With<PublishedArticle>().With<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).CountAsync());
                Assert.Equal(2, await session.Query().For<Article>().With<PublishedArticle>().With<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).CountAsync());
                Assert.Equal(0, await session.Query().For<Article>().With<PublishedArticle>().With<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).CountAsync());
            }
        }

        [Fact]
        public async Task LoadingDocumentShouldNotDuplicateIndex()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                };

                session.Save(bill);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<PersonByName>().CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>(x => x.SomeName == "Bill").CountAsync());

                var person = await session
                    .Query<Person, PersonByName>()
                    .Where(x => x.SomeName == "Bill")
                    .FirstOrDefaultAsync();

                Assert.NotNull(person);
                Assert.Equal("Bill", person.Firstname);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<PersonByName>().CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>(x => x.SomeName == "Bill").CountAsync());

                var person = await session
                    .Query<Person, PersonByName>()
                    .Where(x => x.SomeName == "Bill")
                    .FirstOrDefaultAsync();

                Assert.NotNull(person);
                Assert.Equal("Bill", person.Firstname);
            }
        }
        [Fact]
        public async Task ShouldIncrementAttachmentIndex()
        {
            _store.RegisterIndexes<AttachmentByDayProvider>();
            //Create one Email with 3 attachments
            using (var session = _store.CreateSession())
            {
                var email = new Email() { Date = new DateTime(2018, 06, 11), Attachements = new System.Collections.Generic.List<Attachement>(){ new Attachement("A1"), new Attachement("A2"), new Attachement("A3") }};
                session.Save(email);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<AttachmentByDay>().CountAsync());
                Assert.Equal(3, (await session.QueryIndex<AttachmentByDay>(x => x.Date == new DateTime(2018, 06, 11).DayOfYear).FirstOrDefaultAsync()).Count);
            }
        }

        [Fact]
        public async Task ShouldUpdateAttachmentIndex()
        {
            _store.RegisterIndexes<AttachmentByDayProvider>();
            var date = new DateTime(2018, 06, 11);

            // Create one Email with 3 attachments
            using (var session = _store.CreateSession())
            {
                var email = new Email()
                {
                    Date = date,
                    Attachements = new List<Attachement>()
                    {
                        new Attachement("A1"),
                        new Attachement("A2"),
                        new Attachement("A3")
                    }
                };

                session.Save(email);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<AttachmentByDay>().CountAsync());
                Assert.Equal(3, (await session.QueryIndex<AttachmentByDay>(x => x.Date == date.DayOfYear).FirstOrDefaultAsync()).Count);
            }

            // Updating existing email, adding 2 attachments
            using (var session = _store.CreateSession())
            {
                var email = await session.Query<Email, AttachmentByDay>()
                    .Where(m => m.Date == date.DayOfYear)
                    .FirstOrDefaultAsync();

                email.Attachements.Add(new Attachement("A4"));
                email.Attachements.Add(new Attachement("A5"));

                session.Save(email);
            }

            // Actual email should be updated, and there should still be a single AttachmentByDay
            using (var session = _store.CreateSession())
            {
                var email = await session.Query<Email, AttachmentByDay>()
                    .Where(m => m.Date == date.DayOfYear)
                    .FirstOrDefaultAsync();

                Assert.Equal(5, email.Attachements.Count);
                Assert.Equal(1, await session.QueryIndex<AttachmentByDay>().CountAsync());
            }

            // AttachmentByDay Count should have been incremented
            using (var session = _store.CreateSession())
            {
                var abd = await session.QueryIndex<AttachmentByDay>(x => x.Date == date.DayOfYear).FirstOrDefaultAsync();
                Assert.Equal(5, abd.Count);
            }

        }
        [Fact]
        public async Task ShouldReduce()
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

            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(4, await session.QueryIndex<ArticlesByDay>().CountAsync());

                Assert.Equal(1, await session.QueryIndex<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).CountAsync());
                Assert.Equal(1, await session.QueryIndex<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).CountAsync());
                Assert.Equal(1, await session.QueryIndex<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).CountAsync());
                Assert.Equal(1, await session.QueryIndex<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).CountAsync());

                Assert.Equal(4, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).CountAsync());
                Assert.Equal(3, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).CountAsync());
                Assert.Equal(2, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).CountAsync());
                Assert.Equal(1, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).CountAsync());
            }
        }

        [Fact]
        public async Task ShouldReduceAndMergeWithDatabase()
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
            }

            using (var session = _store.CreateSession())
            {
                session.Save(new Article { PublishedUtc = new DateTime(2011, 11, 1) });
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(4, await session.QueryIndex<ArticlesByDay>().CountAsync());

                Assert.Equal(1, await session.QueryIndex<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).CountAsync());
                Assert.Equal(1, await session.QueryIndex<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).CountAsync());
                Assert.Equal(1, await session.QueryIndex<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).CountAsync());
                Assert.Equal(1, await session.QueryIndex<ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).CountAsync());

                Assert.Equal(5, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).CountAsync());
                Assert.Equal(3, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).CountAsync());
                Assert.Equal(2, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).CountAsync());
                Assert.Equal(1, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).CountAsync());
            }
        }

        [Fact]
        public async Task MultipleIndexesShouldNotConflict()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Article
                {
                    PublishedUtc = new DateTime(2011, 11, 1)
                });
            }

            using (var session = _store.CreateSession())
            {
                session.Save(new Article
                {
                    PublishedUtc = new DateTime(2011, 11, 1)
                });
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<ArticlesByDay>().CountAsync());
            }
        }

        [Fact]
        public async Task ShouldDeleteCustomObject()
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
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.Query().For<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                session.Delete(person);
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.Query().For<Person>().FirstOrDefaultAsync();
                Assert.Null(person);
            }
        }

        [Fact]
        public async Task RemovingDocumentShouldDeleteMappedIndex()
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
            }

            using (var session = _store.CreateSession())
            {
                var personByName = await session.QueryIndex<PersonByName>().FirstOrDefaultAsync();
                Assert.NotNull(personByName);

                var person = await session.Query().For<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                session.Delete(person);
            }

            using (var session = _store.CreateSession())
            {
                var personByName = await session.QueryIndex<PersonByName>().FirstOrDefaultAsync();
                Assert.Null(personByName);
            }
        }

        [Fact]
        public async Task RemovingDocumentShouldDeleteReducedIndex()
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

            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(10, await session.Query().For<Article>().CountAsync());
                Assert.Equal(4, await session.QueryIndex<ArticlesByDay>().CountAsync());
            }

            // delete a document
            using (var session = _store.CreateSession())
            {
                var article = await session.Query<Article, ArticlesByDay>().Where(b => b.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).FirstOrDefaultAsync();
                Assert.NotNull(article);
                session.Delete(article);
            }

            // there should be only 3 indexes left
            using (var session = _store.CreateSession())
            {
                // document was deleted
                Assert.Equal(9, await session.Query().For<Article>().CountAsync());
                // index was deleted
                Assert.Equal(3, await session.QueryIndex<ArticlesByDay>().CountAsync());
            }
        }

        [Fact]
        public async Task UpdatingDocumentShouldUpdateReducedIndex()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var dates = new[]
                {
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 1),

                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 2),

                    new DateTime(2011, 11, 3),
                    new DateTime(2011, 11, 3),

                    new DateTime(2011, 11, 4)
                };

                foreach (var date in dates)
                {
                    session.Save(new Article { PublishedUtc = date });
                }

            }

            using (var session = _store.CreateSession())
            {
                // 10 articles
                Assert.Equal(10, await session.Query().For<Article>().CountAsync());

                // 4 indexes as there are 4 different dates
                Assert.Equal(4, await session.QueryIndex<ArticlesByDay>().CountAsync());
            }

            // change the published date of an article
            using (var session = _store.CreateSession())
            {
                var article = await session
                    .Query<Article, ArticlesByDay>()
                    .Where(b => b.DayOfYear == new DateTime(2011, 11, 2).DayOfYear)
                    .FirstOrDefaultAsync();

                Assert.NotNull(article);

                article.PublishedUtc = new DateTime(2011, 11, 3);

                session.Save(article);

            }

            // there should be the same number of indexes
            using (var session = _store.CreateSession())
            {
                Assert.Equal(10, await session.Query().For<Article>().CountAsync());
                Assert.Equal(4, await session.QueryIndex<ArticlesByDay>().CountAsync());

                Assert.Equal(4, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 1).DayOfYear).CountAsync());
                Assert.Equal(2, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 2).DayOfYear).CountAsync());
                Assert.Equal(3, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 3).DayOfYear).CountAsync());
                Assert.Equal(1, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == new DateTime(2011, 11, 4).DayOfYear).CountAsync());
            }
        }

        [Fact]
        public virtual async Task AlteringDocumentShouldUpdateReducedIndex()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var dates = new[]
                {
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 2),
                };

                var articles = dates.Select(x => new Article
                {
                    PublishedUtc = x
                });

                foreach (var article in articles)
                {
                    session.Save(article);
                }

            }

            using (var session = _store.CreateSession())
            {
                // There should be 3 articles
                Assert.Equal(3, await session.Query().For<Article>().CountAsync());

                // There should be 2 groups
                Assert.Equal(2, await session.QueryIndex<ArticlesByDay>().CountAsync());
            }

            // Deleting a document which was the only one in the reduced group
            using (var session = _store.CreateSession())
            {
                var article = await session.Query<Article, ArticlesByDay>()
                    .Where(b => b.DayOfYear == new DateTime(2011, 11, 1).DayOfYear)
                    .FirstOrDefaultAsync();

                Assert.NotNull(article);
                session.Delete(article);
            }

            // Ensure the document and its index have been deleted
            using (var session = _store.CreateSession())
            {
                // There should be 1 article
                Assert.Equal(2, await session.Query<Article>().CountAsync());

                // There should be 1 group
                Assert.Equal(1, await session.QueryIndex<ArticlesByDay>().CountAsync());
            }
        }

        [Fact]
        public async Task IndexHasLinkToDocuments()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var d1 = new Article { PublishedUtc = new DateTime(2011, 11, 1) };
                var d2 = new Article { PublishedUtc = new DateTime(2011, 11, 1) };

                session.Save(d1);
                session.Save(d2);
            }

            using (var session = _store.CreateSession())
            {
                var articles = session.Query().For<Article>();
                Assert.Equal(2, await articles.CountAsync());
            }
        }

        [Fact]
        public async Task ChangesAreAutoFlushed()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var d1 = new Article { PublishedUtc = new DateTime(2011, 11, 1) };
                var d2 = new Article { PublishedUtc = new DateTime(2011, 11, 1) };

                session.Save(d1);
                session.Save(d2);

                var articles = session.Query<Article, ArticlesByDay>(x => x.DayOfYear == 305);
                Assert.Equal(2, await articles.CountAsync());
            }
        }

        [Fact]
        public async Task ShouldOrderOnValueType()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();

            using (var session = _store.CreateSession())
            {
                for (int i = 0; i < 100; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + i,
                        Lastname = "Gates" + i,
                        Age = i
                    };

                    session.Save(person);
                }

            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(100, await session.QueryIndex<PersonByAge>().CountAsync());
                Assert.Equal(0, (await session.QueryIndex<PersonByAge>().OrderBy(x => x.Age).FirstOrDefaultAsync()).Age);
                Assert.Equal(99, (await session.QueryIndex<PersonByAge>().OrderByDescending(x => x.Age).FirstOrDefaultAsync()).Age);
            }
        }

        [Fact]
        public async Task CanCountThenListOrdered()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();

            using (var session = _store.CreateSession())
            {
                for (int i = 0; i < 100; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + i,
                        Lastname = "Gates" + i,
                        Age = i
                    };

                    session.Save(person);
                }
            }

            using (var session = _store.CreateSession())
            {
                var query = session.QueryIndex<PersonByAge>().OrderBy(x => x.Age);

                Assert.Equal(100, await query.CountAsync());
                Assert.Equal(100, (await query.ListAsync()).Count());
            }
        }

        [Fact]
        public async Task ShouldPageResults()
        {
            _store.RegisterIndexes<PersonIndexProvider>();
            var random = new Random();
            using (var session = _store.CreateSession())
            {
                var indices = Enumerable.Range(0, 100).Select(x => x).ToList();

                // Randomize indices
                for (var i = 0; i < 100; i++)
                {
                    var a = random.Next(99);
                    var b = random.Next(99);

                    var tmp = indices[a];
                    indices[a] = indices[b];
                    indices[b] = tmp;
                }

                for (int i = 0; i < 100; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + indices[i].ToString("D2"),
                        Lastname = "Gates" + indices[i].ToString("D2"),
                    };

                    session.Save(person);
                }
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(100, await session.QueryIndex<PersonByName>().CountAsync());
                Assert.Equal(10, (await session.QueryIndex<PersonByName>().OrderBy(x => x.SomeName).Skip(0).Take(10).ListAsync()).Count());
                Assert.Equal(10, (await session.QueryIndex<PersonByName>().OrderBy(x => x.SomeName).Skip(10).Take(10).ListAsync()).Count());
                Assert.Equal(5, (await session.QueryIndex<PersonByName>().OrderBy(x => x.SomeName).Skip(95).Take(10).ListAsync()).Count());
                Assert.Equal(90, (await session.QueryIndex<PersonByName>().OrderBy(x => x.SomeName).Skip(10).ListAsync()).Count());

                var ordered = (await session.QueryIndex<PersonByName>().OrderBy(x => x.SomeName).Skip(95).Take(10).ListAsync()).ToList();

                for (var i = 1; i < ordered.Count; i++)
                {
                    Assert.Equal(1, String.Compare(ordered[i].SomeName, ordered[i - 1].SomeName));
                }
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(100, await session.QueryIndex<PersonByName>().CountAsync());
                Assert.Equal(10, (await session.QueryIndex<PersonByName>().OrderByDescending(x => x.SomeName).Skip(0).Take(10).ListAsync()).Count());
                Assert.Equal(10, (await session.QueryIndex<PersonByName>().OrderByDescending(x => x.SomeName).Skip(10).Take(10).ListAsync()).Count());
                Assert.Equal(5, (await session.QueryIndex<PersonByName>().OrderByDescending(x => x.SomeName).Skip(95).Take(10).ListAsync()).Count());
                Assert.Equal(90, (await session.QueryIndex<PersonByName>().OrderByDescending(x => x.SomeName).Skip(10).ListAsync()).Count());

                var ordered = (await session.QueryIndex<PersonByName>().OrderByDescending(x => x.SomeName).Skip(95).Take(10).ListAsync()).ToList();

                for (var i = 1; i < ordered.Count; i++)
                {
                    Assert.Equal(-1, String.Compare(ordered[i].SomeName, ordered[i - 1].SomeName));
                }
            }

            using (var session = _store.CreateSession())
            {
                var query = session.QueryIndex<PersonByName>().OrderBy(x => x.SomeName).Skip(95).Take(10);

                Assert.Equal(100, await query.CountAsync());

                var ordered = (await query.ListAsync()).ToList();

                Assert.Equal(5, ordered.Count);

                for (var i = 1; i < ordered.Count; i++)
                {
                    Assert.Equal(1, String.Compare(ordered[i].SomeName, ordered[i - 1].SomeName));
                }
            }
        }

        [Fact]
        public async Task ShouldPageWithoutOrder()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                for (int i = 0; i < 100; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + i,
                        Lastname = "Gates" + i,
                    };

                    session.Save(person);
                }
            }

            using (var session = _store.CreateSession())
            {
                // Count() should remove paging and order as it's not supported by some databases
                Assert.Equal(100, await session.Query<Person>().CountAsync());
                Assert.Equal(100, await session.Query<Person>().Skip(10).CountAsync());
                Assert.Equal(100, await session.Query<Person>().Take(10).CountAsync());
                Assert.Equal(100, await session.Query<Person>().Skip(95).Take(10).CountAsync());

                // Using ListAsync().Count() as CountAsync() remove Order
                Assert.Equal(100, (await session.Query<Person>().ListAsync()).Count());
                Assert.Equal(90, (await session.Query<Person>().Skip(10).ListAsync()).Count());
                Assert.Equal(10, (await session.Query<Person>().Take(10).ListAsync()).Count());
                Assert.Equal(5, (await session.Query<Person>().Skip(95).Take(10).ListAsync()).Count());

                Assert.Equal(10, (await session.QueryIndex<PersonByName>().Skip(0).Take(10).ListAsync()).Count());
                Assert.Equal(10, (await session.QueryIndex<PersonByName>().Skip(10).Take(10).ListAsync()).Count());
                Assert.Equal(5, (await session.QueryIndex<PersonByName>().Skip(95).Take(10).ListAsync()).Count());
                Assert.Equal(90, (await session.QueryIndex<PersonByName>().Skip(10).ListAsync()).Count());
                Assert.Equal(10, (await session.QueryIndex<PersonByName>().Take(10).ListAsync()).Count());
            }
        }

        [Fact]
        public async Task PagingShouldNotReturnMoreItemsThanResults()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                for (int i = 0; i < 10; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + i,
                        Lastname = "Gates" + i,
                    };

                    session.Save(person);
                }

            }

            using (var session = _store.CreateSession())
            {
                var persons = await session.Query<Person, PersonByName>().Take(100).ListAsync();
                Assert.Equal(10, persons.Count());
            }
        }

        [Fact]
        public virtual async Task ShouldReturnCachedResults()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                for (int i = 0; i < 10; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + i,
                        Lastname = "Gates" + i,
                    };

                    session.Save(person);
                }

            }

            using (var session = _store.CreateSession())
            {
                var persons = await session.Query<Person, PersonByName>().ListAsync();
                Assert.Equal(10, persons.Count());

                persons = await session.Query<Person, PersonByName>().ListAsync();
                Assert.Equal(10, persons.Count());
            }
        }


        [Fact]
        public async Task ShouldQueryByMappedIndex()
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
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query().For<Person>().With<PersonByName>().CountAsync());
                Assert.Equal(1, await session.Query().For<Person>().With<PersonByName>(x => x.SomeName == "Steve").CountAsync());
                Assert.Equal(1, await session.Query().For<Person>().With<PersonByName>().Where(x => x.SomeName == "Steve").CountAsync());
            }
        }

        [Fact]
        public async Task ShouldQueryByReducedIndex()
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

            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(10, await session.Query().For<Article>().With<ArticlesByDay>().CountAsync());

                Assert.Equal(4, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == 305).CountAsync());
                Assert.Equal(3, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == 306).CountAsync());
                Assert.Equal(2, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == 307).CountAsync());
                Assert.Equal(1, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == 308).CountAsync());

                Assert.Equal(7, await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == 305 || x.DayOfYear == 306).CountAsync());
                Assert.Equal(7, (await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == 305 || x.DayOfYear == 306).ListAsync()).Count());

            }
        }

        [Fact]
        public void ShouldSaveBigDocuments()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = new String('x', 10000),
                };


                session.Save(bill);
            }
        }

        [Fact]
        public async Task ShouldResolveTypes()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill"
                };

                session.Save(bill);
            }

            using (var session = _store.CreateSession())
            {
                var bill = await session.Query().Any().FirstOrDefaultAsync();

                Assert.NotNull(bill);
                Assert.IsType<Person>(bill);
                Assert.Equal("Bill", ((Person)bill).Firstname);
            }
        }

        [Fact]
        public async Task ShouldSavePolymorphicProperties()
        {
            using (var session = _store.CreateSession())
            {
                var drawing = new Drawing
                {
                    Shapes = new Shape[]
                    {
                        new Square { Size = 10 },
                        new Square { Size = 20 },
                        new Circle { Radius = 5 }
                    }
                };

                session.Save(drawing);
            }

            using (var session = _store.CreateSession())
            {
                var drawing = await session.Query().For<Drawing>().FirstOrDefaultAsync();

                Assert.NotNull(drawing);
                Assert.Equal(3, drawing.Shapes.Count);
                Assert.Equal(typeof(Square), drawing.Shapes[0].GetType());
                Assert.Equal(typeof(Square), drawing.Shapes[1].GetType());
                Assert.Equal(typeof(Circle), drawing.Shapes[2].GetType());
            }
        }

        [Fact]
        public async Task ShouldIgnoreNonSerializedAttribute()
        {
            using (var session = _store.CreateSession())
            {

                var dog = new Animal
                {
                    Name = "Doggy",
                    Color = "Pink"
                };

                session.Save(dog);
            }

            using (var session = _store.CreateSession())
            {
                var dog = await session.Query().For<Animal>().FirstOrDefaultAsync();

                Assert.NotNull(dog);
                Assert.Equal("Doggy", dog.Name);
                Assert.Null(dog.Color);
            }
        }

        [Fact]
        public async Task ShouldNotHaveWorkAfterCommit()
        {
            using (var session = (Session)_store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);

                Assert.True(session.HasWork());

                await session.CommitAsync();

                Assert.False(session.HasWork());
            }
        }

        [Fact]
        public async Task ShouldGetTypeById()
        {
            int circleId;

            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                await session.CommitAsync();

                circleId = circle.Id;
            }

            using (var session = _store.CreateSession())
            {
                var circle = await session.GetAsync<Circle>(circleId);

                Assert.NotNull(circle);
                Assert.Equal(10, circle.Radius);
            }
        }

        [Fact]
        public async Task ShouldReturnNullWithWrongTypeById()
        {
            int circleId;

            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                await session.CommitAsync();

                circleId = circle.Id;
            }

            using (var session = _store.CreateSession())
            {
                var square = await session.GetAsync<Square>(circleId);

                Assert.Null(square);
            }
        }

        [Fact]
        public virtual async Task ShouldGetDocumentById()
        {
            int circleId;

            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                await session.CommitAsync();

                circleId = circle.Id;
            }

            using (var session = _store.CreateSession())
            {
                var circle = await session.GetAsync<Circle>(circleId);

                Assert.NotNull(circle);
            }
        }

        [Fact]
        public async Task ShouldGetObjectById()
        {
            int circleId;

            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                await session.CommitAsync();

                circleId = circle.Id;
            }

            using (var session = _store.CreateSession())
            {
                var circle = await session.GetAsync<Circle>(circleId);

                Assert.NotNull(circle);
                Assert.Equal(typeof(Circle), circle.GetType());
            }
        }

        [Fact]
        public async Task ShouldGetDynamicById()
        {
            int circleId;

            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                await session.CommitAsync();

                circleId = circle.Id;
            }

            using (var session = _store.CreateSession())
            {
                var circle = await session.GetAsync<dynamic>(circleId);

                Assert.NotNull(circle);
                Assert.Equal(10, (int)circle.Radius);
            }
        }

        [Fact]
        public async Task ShouldAllowMultipleCallToSave()
        {
            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                session.Save(circle);
                session.Save(circle);
            }

            using (var session = _store.CreateSession())
            {
                var circles = await session.Query().For<Circle>().ListAsync();

                Assert.Single(circles);
            }
        }

        [Fact]
        public async Task ShouldUpdateDisconnectedObject()
        {
            Circle circle;
            using (var session = _store.CreateSession())
            {
                circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
            }

            using (var session = _store.CreateSession())
            {
                circle.Radius = 20;
                session.Save(circle);
            }

            using (var session = _store.CreateSession())
            {
                var circles = await session.Query().For<Circle>().ListAsync();
                Assert.Single(circles);
                Assert.Equal(20, circles.FirstOrDefault().Radius);
            }
        }

        [Fact]
        public virtual async Task ShouldNotCommitTransaction()
        {
            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                session.Cancel();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(0, await session.Query().For<Circle>().CountAsync());
            }
        }

        [Fact]
        public async Task ShouldSaveChangesExplicitly()
        {
            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
            }

            using (var session = _store.CreateSession())
            {
                var circle = await session.Query().For<Circle>().FirstOrDefaultAsync();
                Assert.NotNull(circle);

                circle.Radius = 20;
                session.Save(circle);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(20, (await session.Query().For<Circle>().FirstOrDefaultAsync()).Radius);
            }
        }

        [Fact]
        public async Task ShouldNotSaveChangesAutomatically()
        {
            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
            }

            using (var session = _store.CreateSession())
            {
                var circle = await session.Query().For<Circle>().FirstOrDefaultAsync();
                Assert.NotNull(circle);

                circle.Radius = 20;
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(10, (await session.Query().For<Circle>().FirstOrDefaultAsync()).Radius);
            }

            using (var session = _store.CreateSession())
            {
                var circle = await session.Query().For<Circle>().FirstOrDefaultAsync();
                Assert.NotNull(circle);

                circle.Radius = 20;
                session.Save(circle);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(20, (await session.Query().For<Circle>().FirstOrDefaultAsync()).Radius);
            }
        }

        [Fact]
        public async Task ShouldMapWithPredicate()
        {
            _store.RegisterIndexes<PublishedArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Article { IsPublished = true });
                session.Save(new Article { IsPublished = true });
                session.Save(new Article { IsPublished = true });
                session.Save(new Article { IsPublished = true });
                session.Save(new Article { IsPublished = false });
                session.Save(new Article { IsPublished = false });
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(6, await session.Query().For<Article>().CountAsync());
                Assert.Equal(4, await session.Query().For<Article>().With<PublishedArticle>().CountAsync());

                Assert.Equal(4, await session.Query<Article, PublishedArticle>().CountAsync());
            }
        }

        [Fact]
        public async Task ShouldAcceptEmptyIsIn()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill"
                };

                var steve = new Person
                {
                    Firstname = "Steve"
                };

                var paul = new Person
                {
                    Firstname = "Scott"
                };

                session.Save(bill);
                session.Save(steve);
                session.Save(paul);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query().For<Person>()
                    .With<PersonByName>(x => x.SomeName.IsIn(new[] { "Bill", "Steve" }))
                    .CountAsync());

                Assert.Equal(0, await session.Query().For<Person>()
                    .With<PersonByName>(x => x.SomeName.IsIn(new string[0]))
                    .CountAsync());
            }
        }

        [Fact]
        public async Task ShouldCreateNotInQuery()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill"
                };

                var steve = new Person
                {
                    Firstname = "Steve"
                };

                var paul = new Person
                {
                    Firstname = "Scott"
                };

                session.Save(bill);
                session.Save(steve);
                session.Save(paul);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.Query().For<Person>()
                    .With<PersonByName>(x => x.SomeName.IsNotIn(new[] { "Bill", "Steve" }))
                    .CountAsync());

                Assert.Equal(3, await session.Query().For<Person>()
                    .With<PersonByName>(x => x.SomeName.IsNotIn(new string[0]))
                    .CountAsync());
            }
        }

        [Fact]
        public virtual async Task ShouldReadCommittedRecords()
        {
            /*
             * session1 created
             * session1 0 index found
             * session1 save and commit person
             * session1 1 index found (session1 statements flushed)
             * session1 disposed
             * session2 created
             * session2 1 index found (session1 statements isolated)
             * session2 save and commit person
             * session2 2 index found (session2 statements flushed)
             * session2 disposed (session2 transation committed)
             * session2 2 index found
             * session1 2 index found
             */

            _store.RegisterIndexes<PersonIndexProvider>();

            var session1IsDisposed = new ManualResetEvent(false);
            var session2IsDisposed = new ManualResetEvent(false);

            var task1 = Task.Run(async () =>
            {
                using (var session1 = _store.CreateSession(IsolationLevel.ReadCommitted))
                {
                    Assert.Equal(0, await session1.QueryIndex<PersonByName>().CountAsync());

                    var bill = new Person
                    {
                        Firstname = "Bill",
                        Lastname = "Gates",
                    };

                    session1.Save(bill);
                    await session1.CommitAsync();

                    Assert.Equal(1, await session1.QueryIndex<PersonByName>().CountAsync());
                }

                session1IsDisposed.Set();

                if (!session2IsDisposed.WaitOne(5000))
                {
                    Assert.True(false, "session2IsDisposed timeout");
                }

                using (var session1 = _store.CreateSession(IsolationLevel.ReadCommitted))
                {
                    Assert.Equal(2, await session1.QueryIndex<PersonByName>().CountAsync());
                }
            });

            var task2 = Task.Run(async () =>
            {
                if (!session1IsDisposed.WaitOne(5000))
                {
                    Assert.True(false, "session1IsDisposed timeout");
                }

                using (var session2 = _store.CreateSession(IsolationLevel.ReadCommitted))
                {
                    Assert.Equal(1, await session2.QueryIndex<PersonByName>().CountAsync());

                    var steve = new Person
                    {
                        Firstname = "Steve",
                        Lastname = "Ballmer",
                    };

                    session2.Save(steve);

                    await session2.CommitAsync();

                    Assert.Equal(2, await session2.QueryIndex<PersonByName>().CountAsync());
                }

                using (var session2 = _store.CreateSession(IsolationLevel.ReadCommitted))
                {
                    Assert.Equal(2, await session2.QueryIndex<PersonByName>().CountAsync());
                }

                session2IsDisposed.Set();

            });

            await Task.WhenAll(task1, task2);
        }

        [Fact]
        public virtual async Task ShouldReadUncommittedRecords()
        {
            /*
             * session1 created
             * session1 0 index found
             * session1 save and commit person
             * session1 1 index found (session1 statements flushed)
             * session2 created
             * session2 1 index found (session1 statements isolated)
             * session2 save and commit person
             * session2 2 index found (session2 statements flushed)
             * session2 disposed (session2 transation committed)
             * session2 2 index found
             * session1 disposed
             * session1 2 index found
             */

            _store.RegisterIndexes<PersonIndexProvider>();

            var session1IsFlushed = new ManualResetEvent(false);
            var session2IsDisposed = new ManualResetEvent(false);

            var task1 = Task.Run(async () =>
            {
                using (var session1 = _store.CreateSession(IsolationLevel.ReadUncommitted))
                {
                    Assert.Equal(0, await session1.QueryIndex<PersonByName>().CountAsync());

                    var bill = new Person
                    {
                        Firstname = "Bill",
                        Lastname = "Gates",
                    };

                    session1.Save(bill);
                    await session1.CommitAsync();

                    Assert.Equal(1, await session1.QueryIndex<PersonByName>().CountAsync());

                    session1IsFlushed.Set();
                    if (!session2IsDisposed.WaitOne(5000))
                    {
                        Assert.True(false, "session2IsDisposed timeout");
                    }
                }

                using (var session1 = _store.CreateSession(IsolationLevel.ReadUncommitted))
                {
                    Assert.Equal(2, await session1.QueryIndex<PersonByName>().CountAsync());
                }
            });

            var task2 = Task.Run(async () =>
            {
                if (!session1IsFlushed.WaitOne(5000))
                {
                    Assert.True(false, "session1IsFlushed timeout");
                }

                using (var session2 = _store.CreateSession(IsolationLevel.ReadUncommitted))
                {
                    Assert.Equal(1, await session2.QueryIndex<PersonByName>().CountAsync());

                    var steve = new Person
                    {
                        Firstname = "Steve",
                        Lastname = "Ballmer",
                    };

                    session2.Save(steve);

                    await session2.CommitAsync();

                    Assert.Equal(2, await session2.QueryIndex<PersonByName>().CountAsync());
                }

                using (var session2 = _store.CreateSession(IsolationLevel.ReadUncommitted))
                {
                    Assert.Equal(2, await session2.QueryIndex<PersonByName>().CountAsync());
                }

                session2IsDisposed.Set();

            });

            await Task.WhenAll(task1, task2);
        }

        [Fact]
        public async Task ShouldSaveInCollections()
        {
            await _store.InitializeCollectionAsync("Collection1");

            using (var session = _store.CreateSession())
            {
                var bill = new
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                session.Save(bill);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.Query().Any().CountAsync());
            }

            using (new NamedCollection("Collection1"))
            {
                using (var session = _store.CreateSession())
                {

                    var steve = new
                    {
                        Firstname = "Steve",
                        Lastname = "Balmer"
                    };

                    session.Save(steve);
                }

                using (var session = _store.CreateSession())
                {
                    Assert.Equal(1, await session.Query().Any().CountAsync());
                }
            }
        }

        [Fact]
        public async Task ShouldFilterMapIndexPerCollection()
        {
            await _store.InitializeCollectionAsync("Collection1");

            using (new NamedCollection("Collection1"))
            {
                using (var session = _store.CreateSession())
                {
                    new SchemaBuilder(session).CreateMapIndexTable(nameof(PersonByNameCol), column => column
                        .Column<string>(nameof(PersonByNameCol.Name))
                        );
                }

                _store.RegisterIndexes<PersonIndexProviderCol>();

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
                }

                using (var session = _store.CreateSession())
                {
                    Assert.Equal(2, await session.Query<Person, PersonByNameCol>().CountAsync());
                    Assert.Equal(1, await session.Query<Person, PersonByNameCol>(x => x.Name == "Steve").CountAsync());
                    Assert.Equal(1, await session.Query<Person, PersonByNameCol>().Where(x => x.Name == "Steve").CountAsync());
                }
            }

            // Store a Person in the default collection
            using (var session = _store.CreateSession())
            {
                var satya = new Person
                {
                    Firstname = "Satya",
                    Lastname = "Nadella",
                };

                session.Save(satya);
            }

            // Ensure the index hasn't been altered
            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.Query<Person>().CountAsync());
                Assert.Equal(2, await session.QueryIndex<PersonByNameCol>().CountAsync());
            }
        }

        [Fact]
        public async Task ShouldGetAndDeletePerCollection()
        {
            await _store.InitializeCollectionAsync("Collection1");

            using (new NamedCollection("Collection1"))
            {
                using (var session = _store.CreateSession())
                {
                    var bill = new Person
                    {
                        Firstname = "Bill",
                        Lastname = "Gates",
                    };

                    session.Save(bill);
                }

                using (var session = _store.CreateSession())
                {
                    var person = await session.Query<Person>().FirstOrDefaultAsync();
                    Assert.NotNull(person);

                    person = await session.GetAsync<Person>(person.Id);
                    Assert.NotNull(person);

                    session.Delete(person);
                }

                using (var session = _store.CreateSession())
                {
                    var person = await session.Query<Person>().FirstOrDefaultAsync();
                    Assert.Null(person);
                }
            }
        }

        [Fact]
        public virtual async Task ShouldIndexWithDateTime()
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
                var dates = new[]
                {
                    new DateTime(2011, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2011, 11, 2, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2011, 11, 3, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2011, 11, 4, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2011, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2011, 11, 2, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2011, 11, 3, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2011, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2011, 11, 2, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2011, 11, 1, 0, 0, 0, DateTimeKind.Utc)
                };

                var articles = dates.Select((x, i) => new Article
                {
                    PublishedUtc = x
                });


                foreach (var article in articles)
                {
                    session.Save(article);
                }
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(10, await session.QueryIndex<ArticleByPublishedDate>().CountAsync());
                Assert.Equal(10, (await session.QueryIndex<ArticleByPublishedDate>().ListAsync()).Count());
                Assert.Equal(4, await session.QueryIndex<ArticleByPublishedDate>(x => x.PublishedDateTime == new DateTime(2011, 11, 1, 0, 0, 0, DateTimeKind.Utc)).CountAsync());

                var list = await session.QueryIndex<ArticleByPublishedDate>(x => x.PublishedDateTime < new DateTime(2011, 11, 2, 0, 0, 0, DateTimeKind.Utc)).ListAsync();
                Assert.Equal(4, list.Count());
            }
        }

        [Fact]
        public virtual async Task ShouldUpdateEntitiesFromSeparateSessions()
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
                Assert.Equal(1, await session.Query<Person, PersonByName>().CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>().Where(x => x.SomeName == "Bill").CountAsync());
            }

            using (var session = _store.CreateSession())
            {
                bill.Firstname = "Bill2";
                session.Save(bill);
                await session.CommitAsync();

                Assert.Equal(1, await session.Query<Person, PersonByName>().CountAsync());
                Assert.Equal(0, await session.QueryIndex<PersonByName>().Where(x => x.SomeName == "Bill").CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>().Where(x => x.SomeName == "Bill2").CountAsync());
            }
        }

        [Fact]
        public async Task PooledSessionsShouldCommit()
        {
            using (var session = _store.CreateSession())
            {
                session.Save(new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                });

                Assert.Equal(1, await session.Query<Person>().CountAsync());
            }

            using (var session = _store.CreateSession())
            {
                session.Save(new Person
                {
                    Firstname = "Bill2",
                    Lastname = "Gates"
                });

                Assert.Equal(2, await session.Query<Person>().CountAsync());
            }
        }

        [Fact]
        public async Task TrackDocumentQuery()
        {
            Person person1, person2;

            using (var session = _store.CreateSession())
            {
                person1 = new Person { Firstname = "Bill" };
                session.Save(person1);
                person2 = await session.Query<Person>().FirstOrDefaultAsync();
            }

            Assert.Equal(person1, person2);
        }

        [Theory]
        [InlineData("second", 4)]
        [InlineData("minute", 5)]
        [InlineData("hour", 2)]
        [InlineData("day", 1)]
        [InlineData("month", 11)]
        [InlineData("year", 2011)]
        public async Task SqlDateFunctions(string method, int expected)
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
                var article = new Article
                {
                    PublishedUtc = new DateTime(2011, 11, 1, 2, 5, 4, DateTimeKind.Utc)
                };

                session.Save(article);
            }

            int result;

            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                var dialect = SqlDialectFactory.For(connection);
                var sql = "SELECT " + dialect.RenderMethod(method, dialect.QuoteForColumnName(nameof(ArticleByPublishedDate.PublishedDateTime))) + " FROM " + dialect.QuoteForTableName(TablePrefix + nameof(ArticleByPublishedDate));
                result = await connection.QueryFirstOrDefaultAsync<int>(sql);
            }

            Assert.Equal(expected, result);
        }

        [Fact]
        public virtual async Task CanUseStaticMethodsInLinqQueries()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            var bill = new Person
            {
                Firstname = "BILL",
                Lastname = "GATES"
            };

            using (var session = _store.CreateSession())
            {
                session.Save(bill);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<PersonByName>().Where(x => x.SomeName == PersonByName.Normalize(bill.Firstname)).CountAsync());
            }
        }

        [Fact]
        public virtual async Task ShouldRemoveGroupKey()
        {
            _store.RegisterIndexes<UserByRoleNameIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var user = new User
                {
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    RoleNames = { "administrator", "editor" }
                };

                session.Save(user);
            }

            using (var session = _store.CreateSession())
            {
                var user = await session.Query<User>().FirstOrDefaultAsync();
                user.RoleNames.Remove("editor");
                session.Save(user);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<UserByRoleNameIndex>().CountAsync());
                Assert.Equal(1, await session.Query<User, UserByRoleNameIndex>(x => x.RoleName == "administrator").CountAsync());
                Assert.Equal(0, await session.Query<User, UserByRoleNameIndex>(x => x.RoleName == "editor").CountAsync());
            }
        }

        [Fact]
        public virtual async Task ShouldAddGroupKey()
        {
            _store.RegisterIndexes<UserByRoleNameIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var user = new User
                {
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    RoleNames = { "administrator" }
                };

                session.Save(user);
            }

            using (var session = _store.CreateSession())
            {
                var user = await session.Query<User>().FirstOrDefaultAsync();
                user.RoleNames.Add("editor");
                session.Save(user);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.QueryIndex<UserByRoleNameIndex>().CountAsync());
                Assert.Equal(1, await session.Query<User, UserByRoleNameIndex>(x => x.RoleName == "administrator").CountAsync());
                Assert.Equal(1, await session.Query<User, UserByRoleNameIndex>(x => x.RoleName == "editor").CountAsync());
            }
        }

        [Fact]
        public virtual async Task ShouldGateQuery()
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

                for (int i = 0; i < 20; i++)
                {
                    session.Save(new Person { Firstname = $"Foo {i}" });
                }
            }

            var concurrency = 32;
            var MaxTransactions = 100000;

            var counter = 0;
            var stopping = false;

            // Warmup

            var tasks = Enumerable.Range(1, concurrency).Select(i => Task.Run(async () =>
            {
                while (!stopping && Interlocked.Add(ref counter, 1) < MaxTransactions)
                {
                    using (var session = _store.CreateSession())
                    {
                        await session.Query().For<Person>().With<PersonByName>().ListAsync();
                        await session.Query().For<Person>().With<PersonByName>(x => x.SomeName == "Steve").ListAsync();
                        await session.Query().For<Person>().With<PersonByName>().Where(x => x.SomeName == "Steve").ListAsync();
                    }
                }
            })).ToList();

            tasks.Add(Task.Delay(TimeSpan.FromSeconds(3)));

            await Task.WhenAny(tasks);

            // Flushing tasks
            stopping = true;
            await Task.WhenAll(tasks);
            stopping = false;
            counter = 0;

            // Gated queries

            tasks = Enumerable.Range(1, concurrency).Select(i => Task.Run(async () =>
            {
                while (!stopping && Interlocked.Add(ref counter, 1) < MaxTransactions)
                {
                    using (var session = _store.CreateSession())
                    {
                        await session.Query().For<Person>().With<PersonByName>().ListAsync();
                        await session.Query().For<Person>().With<PersonByName>(x => x.SomeName == "Steve").ListAsync();
                        await session.Query().For<Person>().With<PersonByName>().Where(x => x.SomeName == "Steve").ListAsync();
                    }
                }
            })).ToList();

            tasks.Add(Task.Delay(TimeSpan.FromSeconds(3)));

            await Task.WhenAny(tasks);

            var gatedCounter = counter;

            // Flushing tasks
            stopping = true;
            await Task.WhenAll(tasks);
            stopping = false;
            counter = 0;

            // Non-gated queries

            _store.Configuration.DisableQueryGating();

            tasks = Enumerable.Range(1, concurrency).Select(i => Task.Run(async () =>
            {
                while (!stopping && Interlocked.Add(ref counter, 1) < MaxTransactions)
                {
                    using (var session = _store.CreateSession())
                    {
                        await session.Query().For<Person>().With<PersonByName>().ListAsync();
                        await session.Query().For<Person>().With<PersonByName>(x => x.SomeName == "Steve").ListAsync();
                        await session.Query().For<Person>().With<PersonByName>().Where(x => x.SomeName == "Steve").ListAsync();
                    }
                }
            })).ToList();

            tasks.Add(Task.Delay(TimeSpan.FromSeconds(3)));

            await Task.WhenAny(tasks);

            var nonGatedCounter = counter;

            // Flushing tasks
            stopping = true;
            await Task.WhenAll(tasks);
            stopping = false;

            // Not running the statement in case it fails (non deterministic)
            // Assert.True(gatedCounter > nonGatedCounter);

            Console.WriteLine($"Gated: {gatedCounter} NonGated: {nonGatedCounter}");
        }

        [Fact]
        public virtual async Task ShouldPopulateIdFieldWithPrivateSetter()
        {
            Tree oak;

            using (var session = _store.CreateSession())
            {
                oak = new Tree
                {
                    Type = "Oak",
                    HeightInInches = 375
                };

                session.Save(oak);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(oak.Id, (await session.Query<Tree>().FirstOrDefaultAsync()).Id);
            }
        }

        [Fact]
        public virtual async Task ShouldConvertDateTimeToUtc()
        {
            using (var session = _store.CreateSession())
            {
                session.Save(new Article { PublishedUtc = new DateTime(2013, 1, 21, 0, 0, 0, DateTimeKind.Local) });
            }

            using (var session = _store.CreateSession())
            {
                var article = await session.Query<Article>().FirstOrDefaultAsync();

                Assert.NotNull(article);
                Assert.Equal(DateTimeKind.Utc, article.PublishedUtc.Kind);
                Assert.Equal(new DateTime(2013, 1, 21, 0, 0, 0, DateTimeKind.Local).ToUniversalTime(), article.PublishedUtc.ToUniversalTime());
            }
        }

        [Fact]
        public async Task ShouldOrderCaseInsensitively()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Person { Firstname = "D" });
                session.Save(new Person { Firstname = "b" });
                session.Save(new Person { Firstname = "G" });
                session.Save(new Person { Firstname = "F" });
                session.Save(new Person { Firstname = "c" });
                session.Save(new Person { Firstname = "e" });
                session.Save(new Person { Firstname = "A" });
            }

            using (var session = _store.CreateSession())
            {
                var results = await session.Query<Person, PersonByName>().OrderBy(x => x.SomeName).ListAsync();

                Assert.Equal("A", results.ElementAt(0).Firstname);
                Assert.Equal("b", results.ElementAt(1).Firstname);
                Assert.Equal("c", results.ElementAt(2).Firstname);
                Assert.Equal("D", results.ElementAt(3).Firstname);
                Assert.Equal("e", results.ElementAt(4).Firstname);
                Assert.Equal("F", results.ElementAt(5).Firstname);
                Assert.Equal("G", results.ElementAt(6).Firstname);
            }
        }

        [Fact]
        public async Task ShouldImportDetachedObject()
        {
            var bill = new Person
            {
                Firstname = "Bill",
            };

            using (var session = _store.CreateSession())
            {

                session.Save(bill);

                Assert.Single(await session.Query<Person>().ListAsync());
                Assert.True(bill.Id > 0);
            }

            using (var session = _store.CreateSession())
            {
                bill = new Person
                {
                    Id = bill.Id,
                    Firstname = "Bill",
                    Lastname = "Gates",
                };

                session.Import(bill);
            }

            using (var session = _store.CreateSession())
            {
                var all = await session.Query<Person>().ListAsync();
                Assert.Single(all);
                Assert.Equal("Gates", all.First().Lastname);
            }
        }
    }
}
