using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using YesSql.Commands;
using YesSql.Filters.Query;
using YesSql.Indexes;
using YesSql.Services;
using YesSql.Sql;
using YesSql.Tests.Commands;
using YesSql.Tests.CompiledQueries;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    public abstract class CoreTests : IAsyncLifetime
    {
        protected virtual string TablePrefix => "tp";

        protected virtual string DecimalColumnDefinitionFormatString => "DECIMAL({0},{1})";

        protected IStore _store;
        protected static IConfiguration _configuration;

        protected ITestOutputHelper _output;
        protected abstract IConfiguration CreateConfiguration();


        public CoreTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public async Task InitializeAsync()
        {
            // Create the tables only once
            if (_configuration == null)
            {
                _configuration = CreateConfiguration();

                CleanDatabase(_configuration, false);

                _store = await StoreFactory.CreateAndInitializeAsync(_configuration);
                await _store.InitializeCollectionAsync("Col1");
                _store.TypeNames[typeof(Person)] = "People";

                CreateTables(_configuration);
            }
            else
            {
                _store = await StoreFactory.CreateAndInitializeAsync(_configuration);
                await _store.InitializeCollectionAsync("Col1");
                _store.TypeNames[typeof(Person)] = "People";
            }

            // Clear the tables for each new test
            ClearTables(_configuration);
        }

        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        //[DebuggerNonUserCode]
        protected virtual void CleanDatabase(IConfiguration configuration, bool throwOnError)
        {
            // Remove existing tables
            using var connection = configuration.ConnectionFactory.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction(configuration.IsolationLevel);
            var builder = new SchemaBuilder(configuration, transaction, throwOnError);

            builder.DropReduceIndexTable<ArticlesByDay>();
            builder.DropReduceIndexTable<AttachmentByDay>();
            builder.DropMapIndexTable<ArticleByPublishedDate>();
            builder.DropMapIndexTable<PersonByName>();
            builder.DropMapIndexTable<CarIndex>();
            builder.DropMapIndexTable<PersonByNameCol>();
            builder.DropMapIndexTable<PersonIdentity>();
            builder.DropMapIndexTable<EmailByAttachment>();
            builder.DropMapIndexTable<TypesIndex>();

            builder.DropMapIndexTable<ShapeIndex>();
            builder.DropMapIndexTable<PersonByAge>();
            builder.DropMapIndexTable<PersonByNullableAge>();
            builder.DropMapIndexTable<Binary>();
            builder.DropMapIndexTable<PublishedArticle>();
            builder.DropMapIndexTable<PropertyIndex>();
            builder.DropReduceIndexTable<UserByRoleNameIndex>();

            builder.DropMapIndexTable<PersonByName>("Col1");
            builder.DropMapIndexTable<PersonByNameCol>("Col1");
            builder.DropMapIndexTable<PersonByBothNamesCol>("Col1");
            builder.DropReduceIndexTable<PersonsByNameCol>("Col1");

            builder.DropTable(configuration.TableNameConvention.GetDocumentTable("Col1"));
            builder.DropTable(configuration.TableNameConvention.GetDocumentTable(""));

            builder.DropTable(DbBlockIdGenerator.TableName);

            OnCleanDatabase(builder, transaction);

            transaction.Commit();
        }

        protected virtual void ClearTables(IConfiguration configuration)
        {
            void DeleteReduceIndexTable<IndexType>(DbConnection connection, string collection = "")
            {
                var indexTable = configuration.TableNameConvention.GetIndexTable(typeof(IndexType), collection);
                var documentTable = configuration.TableNameConvention.GetDocumentTable(collection);

                var bridgeTableName = indexTable + "_" + documentTable;

                try
                {
                    connection.Execute($"DELETE FROM {configuration.SqlDialect.QuoteForTableName(TablePrefix + bridgeTableName)}");
                    connection.Execute($"DELETE FROM {configuration.SqlDialect.QuoteForTableName(TablePrefix + indexTable)}");
                }
                catch
                {
                }
            }

            void DeleteMapIndexTable<IndexType>(DbConnection connection, string collection = "")
            {
                var indexName = typeof(IndexType).Name;
                var indexTable = configuration.TableNameConvention.GetIndexTable(typeof(IndexType), collection);

                try
                {
                    connection.Execute($"DELETE FROM {configuration.SqlDialect.QuoteForTableName(TablePrefix + indexTable)}");
                }
                catch { }
            }

            void DeleteDocumentTable(DbConnection connection, string collection = "")
            {
                var tableName = configuration.TableNameConvention.GetDocumentTable(collection);

                try
                {
                    connection.Execute($"DELETE FROM {configuration.SqlDialect.QuoteForTableName(TablePrefix + tableName)}");
                }
                catch { }
            }

            // Remove existing tables
            using (var connection = configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                DeleteReduceIndexTable<ArticlesByDay>(connection);
                DeleteReduceIndexTable<AttachmentByDay>(connection);
                DeleteMapIndexTable<ArticleByPublishedDate>(connection);
                DeleteMapIndexTable<PersonByName>(connection);
                DeleteMapIndexTable<CarIndex>(connection);
                DeleteMapIndexTable<PersonByNameCol>(connection);
                DeleteMapIndexTable<PersonIdentity>(connection);
                DeleteMapIndexTable<EmailByAttachment>(connection);
                DeleteMapIndexTable<TypesIndex>(connection);

                DeleteMapIndexTable<ShapeIndex>(connection);
                DeleteMapIndexTable<PersonByAge>(connection);
                DeleteMapIndexTable<PersonByNullableAge>(connection);
                DeleteMapIndexTable<Binary>(connection);
                DeleteMapIndexTable<PublishedArticle>(connection);
                DeleteMapIndexTable<PropertyIndex>(connection);
                DeleteReduceIndexTable<UserByRoleNameIndex>(connection);

                DeleteMapIndexTable<PersonByName>(connection, "Col1");
                DeleteMapIndexTable<PersonByNameCol>(connection, "Col1");
                DeleteMapIndexTable<PersonByBothNamesCol>(connection, "Col1");
                DeleteReduceIndexTable<PersonsByNameCol>(connection, "Col1");

                DeleteDocumentTable(connection, "Col1");
                DeleteDocumentTable(connection, "");

                //connection.Execute($"DELETE FROM {TablePrefix}{DbBlockIdGenerator.TableName}");

                OnClearTables(connection);

            }
        }

        protected virtual void OnCleanDatabase(SchemaBuilder builder, DbTransaction transaction)
        {

        }

        protected virtual void OnClearTables(DbConnection connection)
        {

        }

        public void CreateTables(IConfiguration configuration)
        {
            using var connection = configuration.ConnectionFactory.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction(configuration.IsolationLevel);
            var builder = new SchemaBuilder(configuration, transaction);

            builder.CreateReduceIndexTable<ArticlesByDay>(column => column
                    .Column<int>(nameof(ArticlesByDay.Count))
                    .Column<int>(nameof(ArticlesByDay.DayOfYear))
                );
            builder.CreateReduceIndexTable<AttachmentByDay>(column => column
                    .Column<int>(nameof(AttachmentByDay.Count))
                    .Column<int>(nameof(AttachmentByDay.Date))
                );

            builder.CreateReduceIndexTable<UserByRoleNameIndex>(column => column
                    .Column<int>(nameof(UserByRoleNameIndex.Count))
                    .Column<string>(nameof(UserByRoleNameIndex.RoleName))
                );

            builder.CreateMapIndexTable<ArticleByPublishedDate>(column => column
                    .Column<DateTime>(nameof(ArticleByPublishedDate.PublishedDateTime))
                    .Column<string>(nameof(ArticleByPublishedDate.Title))
                );

            builder.CreateMapIndexTable<PersonByName>(column => column
                    .Column<string>(nameof(PersonByName.SomeName))
                );

            builder.CreateMapIndexTable<CarIndex>(column => column
                    .Column<string>(nameof(CarIndex.Name))
                    .Column<Categories>(nameof(CarIndex.Category))
                );

            builder.CreateMapIndexTable<PersonByNameCol>(column => column
                    .Column<string>(nameof(PersonByNameCol.Name))
                    );

            builder.CreateMapIndexTable<PersonIdentity>(column => column
                    .Column<string>(nameof(PersonIdentity.Identity))
                );

            builder.CreateMapIndexTable<PersonByAge>(column => column
                    .Column<int>(nameof(PersonByAge.Age))
                    .Column<bool>(nameof(PersonByAge.Adult))
                    .Column<string>(nameof(PersonByAge.Name))
                );

            builder.CreateMapIndexTable<ShapeIndex>(column => column
                    .Column<string>(nameof(ShapeIndex.Name))
                );

            builder.CreateMapIndexTable<PersonByNullableAge>(column => column
                    .Column<int?>(nameof(PersonByAge.Age), c => c.Nullable())
                );

            builder.CreateMapIndexTable<PublishedArticle>(column => { });

            builder.CreateMapIndexTable<EmailByAttachment>(column => column
                    .Column<DateTime>(nameof(EmailByAttachment.Date))
                    .Column<string>(nameof(EmailByAttachment.AttachmentName))
                );

            builder.CreateMapIndexTable<PropertyIndex>(column => column
                .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(767))
                .Column<bool>(nameof(PropertyIndex.ForRent))
                .Column<bool>(nameof(PropertyIndex.IsOccupied))
                .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
            );

            builder.CreateMapIndexTable<Binary>(column => column
                    .Column<byte[]>(nameof(Binary.Content1), c => c.WithLength(255))
                    .Column<byte[]>(nameof(Binary.Content2), c => c.WithLength(8000))
                    .Column<byte[]>(nameof(Binary.Content3), c => c.WithLength(65535))
                    .Column<byte[]>(nameof(Binary.Content4), c => c.WithLength(1))
                );

            builder.CreateMapIndexTable<TypesIndex>(column => column
                    .Column<bool>(nameof(TypesIndex.ValueBool))
                    //.Column<char>(nameof(TypesIndex.ValueChar))
                    .Column<DateTime>(nameof(TypesIndex.ValueDateTime))
                    .Column<DateTimeOffset>(nameof(TypesIndex.ValueDateTimeOffset))
                    .Column<decimal>(nameof(TypesIndex.ValueDecimal))
                    .Column<double>(nameof(TypesIndex.ValueDouble))
                    .Column<float>(nameof(TypesIndex.ValueFloat))
                    .Column<Guid>(nameof(TypesIndex.ValueGuid))
                    .Column<int>(nameof(TypesIndex.ValueInt))
                    .Column<long>(nameof(TypesIndex.ValueLong))
                    //.Column<sbyte>(nameof(TypesIndex.ValueSByte))
                    .Column<short>(nameof(TypesIndex.ValueShort))
                    .Column<TimeSpan>(nameof(TypesIndex.ValueTimeSpan))
                    //.Column<uint>(nameof(TypesIndex.ValueUInt))
                    //.Column<ulong>(nameof(TypesIndex.ValueULong))
                    //.Column<ushort>(nameof(TypesIndex.ValueUShort))
                    .Column<bool?>(nameof(TypesIndex.NullableBool), c => c.Nullable())
                    //.Column<char?>(nameof(TypesIndex.NullableChar), c => c.Nullable())
                    .Column<DateTime?>(nameof(TypesIndex.NullableDateTime), c => c.Nullable())
                    .Column<DateTimeOffset?>(nameof(TypesIndex.NullableDateTimeOffset), c => c.Nullable())
                    .Column<decimal?>(nameof(TypesIndex.NullableDecimal), c => c.Nullable())
                    .Column<double?>(nameof(TypesIndex.NullableDouble), c => c.Nullable())
                    .Column<float?>(nameof(TypesIndex.NullableFloat), c => c.Nullable())
                    .Column<Guid?>(nameof(TypesIndex.NullableGuid), c => c.Nullable())
                    .Column<int?>(nameof(TypesIndex.NullableInt), c => c.Nullable())
                    .Column<long?>(nameof(TypesIndex.NullableLong), c => c.Nullable())
                    //.Column<sbyte?>(nameof(TypesIndex.NullableSByte), c => c.Nullable())
                    .Column<short?>(nameof(TypesIndex.NullableShort), c => c.Nullable())
                    .Column<TimeSpan?>(nameof(TypesIndex.NullableTimeSpan), c => c.Nullable())
                //.Column<uint?>(nameof(TypesIndex.NullableUInt), c => c.Nullable())
                //.Column<ulong?>(nameof(TypesIndex.NullableULong), c => c.Nullable())
                //.Column<ushort?>(nameof(TypesIndex.NullableUShort), c => c.Nullable())
                );

            builder.CreateMapIndexTable<PersonByName>(column => column
                    .Column<string>(nameof(PersonByName.SomeName)),
                    "Col1"
                    );

            builder.CreateMapIndexTable<PersonByNameCol>(column => column
                    .Column<string>(nameof(PersonByNameCol.Name)),
                    "Col1"
                    );

            builder.CreateMapIndexTable<PersonByBothNamesCol>(column => column
                    .Column<string>(nameof(PersonByBothNamesCol.Firstname))
                    .Column<string>(nameof(PersonByBothNamesCol.Lastname)),
                    "Col1"
                    );

            builder.CreateReduceIndexTable<PersonsByNameCol>(column => column
                    .Column<string>(nameof(PersonsByNameCol.Name))
                    .Column<int>(nameof(PersonsByNameCol.Count)),
                    "Col1"
                    );

            transaction.Commit();
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
        public async Task ShouldSaveCustomObject()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                session.Save(bill);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.Query<Person>().CountAsync());
            }
        }

        [Fact]
        public async Task NotCallingCommitShouldCancelTransaction()
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

            using (var session = _store.CreateSession())
            {
                Assert.Equal(0, await session.Query<Person>().CountAsync());
            }
        }

        [Fact]
        public async Task ShouldCancelTransactionAfterFlush()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                session.Save(bill);

                await session.FlushAsync();

                var steve = new Person
                {
                    Firstname = "Steve",
                    Lastname = "Balmer"
                };

                session.Save(steve);
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(0, await session.Query<Person>().CountAsync());
            }
        }

        [Fact]
        public async Task ShouldSaveSeveralObjects()
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query<Person>().CountAsync());
            }
        }

        [Fact]
        public async Task ShouldSaveAnonymousObject()
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

                await session.SaveChangesAsync();
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
                await session.SaveChangesAsync();
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
                await session.SaveChangesAsync();
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
                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.QueryIndex<PersonByName>().Where(d => d.QuoteForColumnName(nameof(PersonByName.SomeName)) + " = @Name").WithParameter("Name", "Bill").FirstOrDefaultAsync();

                Assert.NotNull(person);
                Assert.Equal("Bill", (string)person.SomeName);
            }
        }

        [Fact]
        public async Task ShouldMapEnums()
        {
            _store.RegisterIndexes<CarIndexProvider>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Car { Name = "Truck", Category = Categories.Truck });
                session.Save(new Car { Name = "Van", Category = Categories.Van });

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal("Van", (await session.QueryIndex<CarIndex>(x => x.Category == Categories.Van).FirstOrDefaultAsync()).Name);
                Assert.Equal("Truck", (await session.QueryIndex<CarIndex>(x => x.Category == Categories.Truck).FirstOrDefaultAsync()).Name);

                Assert.Equal("Van", (await session.Query<Car, CarIndex>(x => x.Category == Categories.Van).FirstOrDefaultAsync()).Name);
                Assert.Equal("Truck", (await session.Query<Car, CarIndex>(x => x.Category == Categories.Truck).FirstOrDefaultAsync()).Name);
            }
        }

        [Fact]
        public async Task ShouldApplyIndexFilter()
        {
            _store.RegisterIndexes<PersonWithAIndexProvider>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Person { Firstname = "Alex" });
                session.Save(new Person { Firstname = "Bill" });
                session.Save(new Person { Firstname = "assan" });

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.QueryIndex<PersonByName>().CountAsync());
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                session.RegisterIndexes(new ScopedPersonAsyncIndexProvider(2));

                session.Save(new Person { Firstname = "Bill" });
                session.Save(new Person { Firstname = "Steve" });

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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
        public async Task ShouldQueryNullVariables()
        {
            var logger = new ConsoleLogger(_output);

            _store.Configuration.Logger = logger;


            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Person { Firstname = null });
                session.Save(new Person { Firstname = "a" });
                session.Save(new Person { Firstname = "b" });

                await session.SaveChangesAsync();
            }

            string nullVariable = null;

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.QueryIndex<PersonByName>(x => x.SomeName == nullVariable).CountAsync());
                Assert.Equal(2, await session.QueryIndex<PersonByName>(x => x.SomeName != nullVariable).CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>(x => nullVariable == x.SomeName).CountAsync());
                Assert.Equal(2, await session.QueryIndex<PersonByName>(x => nullVariable != x.SomeName).CountAsync());
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var connection = await session.CreateConnectionAsync();
                var dialect = _store.Configuration.SqlDialect;
                var sql = dialect.QuoteForColumnName(nameof(PersonByName.SomeName)) + " = " + dialect.GetSqlValue("Bill");

                var person = await session.Query<Person, PersonByName>().Where(sql).FirstOrDefaultAsync();

                Assert.NotNull(person);
                Assert.Equal("Bill", person.Firstname);
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
                await session.FlushAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var prod = await session.GetAsync<Product>(productId);
                Assert.NotNull(prod);
                Assert.Equal("Milk", prod.Name);
            }
        }

        [Fact]
        public async Task ShouldAssignIdWhenSaved()
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task NoSavingChangesShouldRollbackAutoFlush()
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

            using (var session = _store.CreateSession())
            {
                Assert.Equal(0, await session.Query<Person>().CountAsync());
            }
        }

        [Fact]
        public async Task ShouldKeepIdentityMapOnCommitAsync()
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

                Assert.Equal(bill, newBill);

                await session.SaveChangesAsync();

                newBill = await session.GetAsync<Person>(bill.Id);

                Assert.Equal(bill, newBill);
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                // IsNotIn
                Assert.Null(await session.Query<Person, PersonByAge>().Where(x => x.Age.IsNotIn(new[] { 12, 50 })).FirstOrDefaultAsync());
                Assert.NotNull(await session.Query<Person, PersonByAge>().Where(x => x.Age.IsNotIn(new[] { 1, 2, 3 }.Cast<object>())).FirstOrDefaultAsync());
            }
        }

        [Fact]
        public async Task ShouldQueryWithCompiledQueries()
        {
            var logger = new ConsoleLogger(_output);

            _store.Configuration.Logger = logger;

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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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
        public async Task ShouldPageCompiledQueries()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();

            using (var session = _store.CreateSession())
            {
                for (var i = 0; i < 10; i++)
                {
                    session.Save(new Person
                    {
                        Firstname = "Bill",
                        Lastname = "Gates",
                        Age = i
                    });
                }

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var results = await session.ExecuteQuery(new PersonPagedQuery()).ListAsync();

                Assert.Single(results);
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(12, (await session.ExecuteQuery(new PersonByNameOrAgeQuery(12, null)).FirstOrDefaultAsync()).Age);
                Assert.Equal(2, (await session.ExecuteQuery(new PersonByNameOrAgeQuery(12, null)).ListAsync()).Count());
            }
        }

        [Fact]
        public async Task ShouldSupportAsyncEnumerable()
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var results = new List<Person>();

                await foreach (var person in session.ExecuteQuery(new PersonByNameOrAgeQuery(12, null)).ToAsyncEnumerable())
                {
                    results.Add(person);
                }

                Assert.Equal(2, results.Count());
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

                await session.SaveChangesAsync();
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
        public async Task ShouldQueryInnerSelectWithNoPredicates()
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query<Person, PersonByAge>().Where(x => x.Name.IsInAny<PersonByName>(y => y.SomeName)).CountAsync());
                Assert.Equal(0, await session.Query<Person, PersonByAge>().Where(x => x.Name.IsNotInAny<PersonByName>(y => y.SomeName)).CountAsync());
            }
        }

        [Fact]
        public async Task ShouldQueryInnerSelectWithUnaryPredicate()
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query<Person, PersonByAge>().Where(x => x.Name.IsIn<PersonByName>(y => y.SomeName, y => true)).CountAsync());
                Assert.Equal(0, await session.Query<Person, PersonByAge>().Where(x => x.Name.IsNotIn<PersonByName>(y => y.SomeName, y => true)).CountAsync());
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

                await session.SaveChangesAsync();
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
        public async Task ShouldQueryWithLike()
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query<Person, PersonByAge>().Where(x => x.Name.IsLike("%l%")).CountAsync());
                Assert.Equal(1, await session.Query<Person, PersonByAge>().Where(x => x.Name.IsNotLike("%B%")).CountAsync());

                Assert.Equal(2, await session.Query<Person, PersonByAge>().Where(x => x.Name.Contains("l")).CountAsync());
                Assert.Equal(1, await session.Query<Person, PersonByAge>().Where(x => x.Name.NotContains("B")).CountAsync());
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

                var firstId = p1.Id;

                bill.Firstname = "Bill2";
                session.Save(bill);

                var p2 = await session.QueryIndex<PersonByName>().FirstOrDefaultAsync();

                Assert.Equal(firstId + 1, p2.Id);

                bill.Firstname = "Bill3";
                session.Save(bill);

                var p3 = await session.QueryIndex<PersonByName>().FirstOrDefaultAsync();

                Assert.Equal(firstId + 2, p3.Id);

                await session.SaveChangesAsync();
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

            await using (var session = _store.CreateSession())
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

                await session.SaveChangesAsync();
            }

            await using (var session = _store.CreateSession())
            {
                Assert.Equal(4, await session.QueryIndex<PersonIdentity>().CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonIdentity>().Where(x => x.Identity == "Hanselman").CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonIdentity>().Where(x => x.Identity == "Guthrie").CountAsync());
                Assert.Equal(2, await session.QueryIndex<PersonIdentity>().Where(x => x.Identity == "Scott").CountAsync());
            }
        }

        [Fact]
        public async Task ShouldQueryMultipleIndexes()
        {
            // We should be able to query documents on multiple rows in an index
            // This mean the same Index table needs to be JOINed

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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query<Person>()
                    .Any(
                        x => x.With<PersonIdentity>(x => x.Identity == "Hanselman"),
                        x => x.With<PersonIdentity>(x => x.Identity == "Guthrie"))
                    .CountAsync()
                    );
            }
        }

        [Fact]
        public async Task ShouldScopeMultipleIndexQueries()
        {
            // When querying multiple indexes the root predicate should be scoped.

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

                var mads = new Person
                {
                    Firstname = "Mads",
                    Lastname = "Kristensen"
                };

                session.Save(hanselman);
                session.Save(guthrie);
                session.Save(mads);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query<Person>()
                    .Any(
                        x => x.With<PersonIdentity>(x => x.Identity == "Hanselman"),
                        x => x.With<PersonIdentity>(x => x.Identity == "Guthrie")
                    )
                    .All(
                        x => x.With<PersonIdentity>(x => x.Identity.IsNotIn<PersonIdentity>(s => s.Identity, s => s.Identity == "Kristensen"))
                    )
                    .CountAsync()
                    );
            }
        }

        [Fact]
        public async Task ShouldScopeNestedMultipleIndexQueries()
        {
            // When querying multiple indexes the current predicate should be scoped.

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

                var mads = new Person
                {
                    Firstname = "Mads",
                    Lastname = "Kristensen"
                };

                session.Save(hanselman);
                session.Save(guthrie);
                session.Save(mads);
                await session.SaveChangesAsync();

            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query<Person>()
                    .All(
                        x => x.Any(
                            x => x.With<PersonIdentity>(x => x.Identity == "Hanselman"),
                            x => x.With<PersonIdentity>(x => x.Identity == "Guthrie")
                        ),
                        x => x.All(
                            x => x.With<PersonIdentity>(x => x.Identity.IsNotIn<PersonIdentity>(s => s.Identity, s => s.Identity == "Kristensen"))
                        )
                    )
                    .CountAsync()
                    );
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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
        public async Task ShouldClearOrders()
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var query = session.Query<Person, PersonByName>().OrderByDescending(x => x.SomeName);
                query.OrderByDescending(x => x.SomeName);

                Assert.Equal("Steve", (await query.FirstOrDefaultAsync()).Firstname);
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

                await session.SaveChangesAsync();
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
        public async Task JoinOrderShouldNotMatter()
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal("Steve", (await session.Query().For<Person>()
                    .With<PersonByName>(x => x.SomeName.StartsWith("S"))
                    .With<PersonByAge>(x => x.Age == 2)
                    .With<PersonByName>(x => x.SomeName.EndsWith("e"))
                    .FirstOrDefaultAsync()).Firstname);
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

                await session.SaveChangesAsync();
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
                var email = new Email() { Date = new DateTime(2018, 06, 11), Attachments = new System.Collections.Generic.List<Attachment>() { new Attachment("A1"), new Attachment("A2"), new Attachment("A3") } };
                session.Save(email);

                await session.SaveChangesAsync();
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
                    Attachments = new List<Attachment>()
                    {
                        new Attachment("A1"),
                        new Attachment("A2"),
                        new Attachment("A3")
                    }
                };

                session.Save(email);

                await session.SaveChangesAsync();
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

                email.Attachments.Add(new Attachment("A4"));
                email.Attachments.Add(new Attachment("A5"));

                session.Save(email);

                await session.SaveChangesAsync();
            }

            // Actual email should be updated, and there should still be a single AttachmentByDay
            using (var session = _store.CreateSession())
            {
                var email = await session.Query<Email, AttachmentByDay>()
                    .Where(m => m.Date == date.DayOfYear)
                    .FirstOrDefaultAsync();

                Assert.Equal(5, email.Attachments.Count);
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                session.Save(new Article { PublishedUtc = new DateTime(2011, 11, 1) });

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                session.Save(new Article
                {
                    PublishedUtc = new DateTime(2011, 11, 1)
                });

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.Query().For<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                session.Delete(person);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.Query().For<Person>().FirstOrDefaultAsync();
                Assert.Null(person);
            }
        }

        [Fact]
        public async Task ShouldDeleteCustomObjectBatch()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                for (var i = 0; i < _configuration.CommandsPageSize + 50; i++)
                {
                    session.Save(new Person
                    {
                        Firstname = "Bill",
                        Lastname = "Gates"
                    });
                }

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var persons = await session.Query().For<Person>().ListAsync();

                foreach (var person in persons)
                {
                    session.Delete(person);
                }

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var personByName = await session.QueryIndex<PersonByName>().FirstOrDefaultAsync();
                Assert.NotNull(personByName);

                var person = await session.Query().For<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                session.Delete(person);

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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
        public async Task AutoflushCanHappenMultipleTimes()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var d1 = new Article { PublishedUtc = new DateTime(2011, 11, 1) };
                var d2 = new Article { PublishedUtc = new DateTime(2011, 11, 1) };

                session.Save(d1);
                session.Save(d2);

                var articles = await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == 305).ListAsync();

                d1.PublishedUtc = new DateTime(2011, 11, 2);

                articles = await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == 306).ListAsync();

                Assert.Single(articles);
            }
        }

        [Fact]
        public async Task ChangesAfterAutoflushAreSaved()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var d1 = new Article { PublishedUtc = new DateTime(2011, 11, 1) };
                var d2 = new Article { PublishedUtc = new DateTime(2011, 11, 1) };

                session.Save(d1);
                session.Save(d2);

                var articles = await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == 305).ListAsync();

                d1.PublishedUtc = new DateTime(2011, 11, 2);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var articles = await session.Query<Article, ArticlesByDay>(x => x.DayOfYear == 306).ListAsync();
                Assert.Single(articles);
            }
        }

        [Fact]
        public async Task ShouldOrderOnValueType()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();

            using (var session = _store.CreateSession())
            {
                for (var i = 0; i < 100; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + i,
                        Lastname = "Gates" + i,
                        Age = i
                    };

                    session.Save(person);
                }

                await session.SaveChangesAsync();
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
                for (var i = 0; i < 100; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + i,
                        Lastname = "Gates" + i,
                        Age = i
                    };

                    session.Save(person);
                }

                await session.SaveChangesAsync();
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

                for (var i = 0; i < 100; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + indices[i].ToString("D2"),
                        Lastname = "Gates" + indices[i].ToString("D2"),
                    };

                    session.Save(person);
                }

                await session.SaveChangesAsync();
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
                for (var i = 0; i < 100; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + i,
                        Lastname = "Gates" + i,
                    };

                    session.Save(person);
                }

                await session.SaveChangesAsync();
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
                for (var i = 0; i < 10; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + i,
                        Lastname = "Gates" + i,
                    };

                    session.Save(person);
                }

                await session.SaveChangesAsync();
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
                for (var i = 0; i < 10; i++)
                {
                    var person = new Person
                    {
                        Firstname = "Bill" + i,
                        Lastname = "Gates" + i,
                    };

                    session.Save(person);
                }

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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
        public async Task ShouldQueryMultipleByReducedIndex()
        {
            _store.RegisterIndexes<ArticleIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var dates = new[]
                {
                    new DateTime(2011, 11, 1),
                    new DateTime(2011, 11, 2),
                    new DateTime(2011, 11, 1),
                };

                var articles = dates.Select(x => new Article
                {
                    PublishedUtc = x
                });

                foreach (var article in articles)
                {
                    session.Save(article);
                }

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var query = session.Query<Article>()
                    .Any(
                        x => x.With<ArticlesByDay>(x => x.DayOfYear == 305),
                        x => x.With<ArticlesByDay>(x => x.DayOfYear == 306)
                    );

                Assert.Equal(3, await query.CountAsync());
            }
        }

        [Fact]
        public async Task ShouldSaveBigDocuments()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = new String('x', 10000),
                };


                session.Save(bill);

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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
        public async Task ShouldQuerySubClasses()
        {
            // When a base type is queried, we need to ensure the 
            // results from the query keep their original type

            _store.RegisterIndexes<ShapeIndexProvider<Circle>>();
            _store.RegisterIndexes<ShapeIndexProvider<Square>>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Square { Size = 10 });
                session.Save(new Square { Size = 20 });
                session.Save(new Circle { Radius = 5 });

                await session.SaveChangesAsync();
            };

            using (var session = _store.CreateSession())
            {
                Assert.Equal(3, await session.QueryIndex<ShapeIndex>().CountAsync());
                Assert.Equal(1, await session.Query<Circle, ShapeIndex>(filterType: true).CountAsync());
                Assert.Equal(2, await session.Query<Square, ShapeIndex>(filterType: true).CountAsync());
                Assert.Equal(3, await session.Query<Shape, ShapeIndex>(filterType: false).CountAsync());

                // In this test, even querying on <object, ShapeIndex> would work
                var shapes = await session.Query<Shape, ShapeIndex>(filterType: false).ListAsync();

                Assert.Equal(3, shapes.Count());
                Assert.Single(shapes.Where(x => x is Circle));
                Assert.Equal(2, shapes.Where(x => x is Square).Count());
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

                await session.SaveChangesAsync();
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
        public async Task ShouldGetTypeById()
        {
            long circleId;

            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                await session.FlushAsync();

                circleId = circle.Id;

                await session.SaveChangesAsync();
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
            long circleId;

            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                await session.FlushAsync();

                circleId = circle.Id;

                await session.SaveChangesAsync();
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
            long circleId;

            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                await session.FlushAsync();

                circleId = circle.Id;

                await session.SaveChangesAsync();
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
            long circleId;

            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                await session.FlushAsync();

                circleId = circle.Id;

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var circle = await session.GetAsync<object>(circleId);

                Assert.NotNull(circle);
                Assert.Equal(typeof(Circle), circle.GetType());
            }
        }

        [Fact]
        public async Task ShouldGetDynamicById()
        {
            long circleId;

            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);
                await session.FlushAsync();

                circleId = circle.Id;

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var circle = await session.GetAsync<dynamic>(circleId);

                Assert.NotNull(circle);
                Assert.Equal(10, (int)circle.Radius);
            }
        }

        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [Theory]
        public async Task ShouldReturnObjectsByIdsInCorrectOrder(int numberOfItems)
        {
            var circleIds = new List<long>();

            using (var session = _store.CreateSession())
            {
                for (var i = 0; i < numberOfItems; i++)
                {
                    var circle = new Circle
                    {
                        Radius = 10
                    };
                    session.Save(circle);
                    circleIds.Add(circle.Id);
                }

                await session.FlushAsync();

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                circleIds.Reverse();

                var circles = await session.GetAsync<object>(circleIds.ToArray());

                Assert.Equal(circleIds, circles.Select(c => ((Circle)c).Id));
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                circle.Radius = 20;
                session.Save(circle);

                await session.SaveChangesAsync();
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
                await session.CancelAsync();

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(0, await session.Query().For<Circle>().CountAsync());
            }
        }

        [Fact]
        public virtual async Task ShouldNotCreatDocumentInCanceledSessions()
        {
            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);

                await session.CancelAsync();

                circle.Radius = 20;

                await session.Query().For<Circle>().CountAsync();

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(0, await session.Query().For<Circle>().CountAsync());
            }
        }

        [Fact]
        public virtual async Task ShouldNotUpdateDocumentInCanceledSessions()
        {
            using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                await session.CancelAsync();

                var circle = await session.Query().For<Circle>().FirstOrDefaultAsync();

                circle.Radius = 20;

                session.Save(circle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {

                var circle = await session.Query().For<Circle>().FirstOrDefaultAsync();

                Assert.Equal(10, circle.Radius);
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var circle = await session.Query().For<Circle>().FirstOrDefaultAsync();
                Assert.NotNull(circle);

                circle.Radius = 20;
                session.Save(circle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(20, (await session.Query().For<Circle>().FirstOrDefaultAsync()).Radius);
            }
        }

        [Fact]
        public async Task ShouldSaveChangesWithIDisposableAsync()
        {
            await using (var session = _store.CreateSession())
            {
                var circle = new Circle
                {
                    Radius = 10
                };

                session.Save(circle);

                await session.SaveChangesAsync();
            }

            await using (var session = _store.CreateSession())
            {
                var circle = await session.Query().For<Circle>().FirstOrDefaultAsync();
                Assert.NotNull(circle);
                circle.Radius = 20;
                session.Save(circle);

                await session.SaveChangesAsync();
            }

            await using (var session = _store.CreateSession())
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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
                // IsolationLevel.ReadCommitted is the default
                using (var session1 = _store.CreateSession())
                {
                    Assert.Equal(0, await session1.QueryIndex<PersonByName>().CountAsync());

                    var bill = new Person
                    {
                        Firstname = "Bill",
                        Lastname = "Gates",
                    };

                    session1.Save(bill);
                    await session1.FlushAsync();

                    Assert.Equal(1, await session1.QueryIndex<PersonByName>().CountAsync());

                    await session1.SaveChangesAsync();
                }

                session1IsDisposed.Set();

                if (!session2IsDisposed.WaitOne(5000))
                {
                    Assert.True(false, "session2IsDisposed timeout");
                }

                // IsolationLevel.ReadCommitted is the default
                using (var session1 = _store.CreateSession())
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

                // IsolationLevel.ReadCommitted is the default
                using (var session2 = _store.CreateSession())
                {
                    Assert.Equal(1, await session2.QueryIndex<PersonByName>().CountAsync());

                    var steve = new Person
                    {
                        Firstname = "Steve",
                        Lastname = "Ballmer",
                    };

                    session2.Save(steve);

                    await session2.FlushAsync();

                    Assert.Equal(2, await session2.QueryIndex<PersonByName>().CountAsync());

                    await session2.SaveChangesAsync();
                }

                // IsolationLevel.ReadCommitted is the default
                using (var session2 = _store.CreateSession())
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
            // Since the default mode is not ReadUncommitted, a specific transaction needs to be started

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
                using (var session1 = _store.CreateSession())
                {
                    await session1.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

                    Assert.Equal(0, await session1.QueryIndex<PersonByName>().CountAsync());

                    var bill = new Person
                    {
                        Firstname = "Bill",
                        Lastname = "Gates",
                    };

                    session1.Save(bill);
                    await session1.FlushAsync();

                    Assert.Equal(1, await session1.QueryIndex<PersonByName>().CountAsync());

                    session1IsFlushed.Set();
                    if (!session2IsDisposed.WaitOne(5000))
                    {
                        Assert.True(false, "session2IsDisposed timeout");
                    }

                    await session1.SaveChangesAsync();
                }

                using (var session1 = _store.CreateSession())
                {
                    await session1.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

                    Assert.Equal(2, await session1.QueryIndex<PersonByName>().CountAsync());

                    await session1.SaveChangesAsync();
                }
            });

            var task2 = Task.Run(async () =>
            {
                if (!session1IsFlushed.WaitOne(5000))
                {
                    Assert.True(false, "session1IsFlushed timeout");
                }

                using (var session2 = _store.CreateSession())
                {
                    await session2.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

                    Assert.Equal(1, await session2.QueryIndex<PersonByName>().CountAsync());

                    var steve = new Person
                    {
                        Firstname = "Steve",
                        Lastname = "Ballmer",
                    };

                    session2.Save(steve);

                    await session2.FlushAsync();

                    Assert.Equal(2, await session2.QueryIndex<PersonByName>().CountAsync());

                    await session2.SaveChangesAsync();
                }

                using (var session2 = _store.CreateSession())
                {
                    await session2.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

                    Assert.Equal(2, await session2.QueryIndex<PersonByName>().CountAsync());

                    await session2.SaveChangesAsync();
                }

                session2IsDisposed.Set();

            });

            await Task.WhenAll(task1, task2);
        }

        [Fact]
        public async Task ShouldSaveInCollections()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                session.Save(bill);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.Query().Any().CountAsync());
            }

            using (var session = _store.CreateSession())
            {

                var steve = new
                {
                    Firstname = "Steve",
                    Lastname = "Balmer"
                };

                session.Save(steve, "Col1");

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.Query("Col1").Any().CountAsync());
            }
        }

        [Fact]
        public async Task ShouldFilterMapIndexPerCollection()
        {
            _store.RegisterIndexes<PersonIndexProviderCol>("Col1");

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

                session.Save(bill, "Col1");
                session.Save(steve, "Col1");

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, await session.Query<Person, PersonByNameCol>("Col1").CountAsync());
                Assert.Equal(1, await session.Query<Person, PersonByNameCol>(x => x.Name == "Steve", "Col1").CountAsync());
                Assert.Equal(1, await session.Query<Person, PersonByNameCol>("Col1").Where(x => x.Name == "Steve").CountAsync());
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

                await session.SaveChangesAsync();
            }

            // Ensure the index hasn't been altered
            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.Query<Person>().CountAsync());
                Assert.Equal(0, await session.QueryIndex<PersonByNameCol>().CountAsync());
            }
        }


        [Fact]
        public async Task ShouldSaveReduceIndexInCollection()
        {
            _store.RegisterIndexes<PersonsByNameIndexProviderCol>("Col1");

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill"
                };

                var bill2 = new Person
                {
                    Firstname = "Bill"
                };

                session.Save(bill, "Col1");
                session.Save(bill2, "Col1");

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(2, (await session.QueryIndex<PersonsByNameCol>(x => x.Name == "Bill", "Col1").FirstOrDefaultAsync()).Count);
            }
        }

        [Fact]
        public async Task ShouldQueryInnerSelectWithCollection()
        {
            _store.RegisterIndexes<PersonIndexProviderCol>("Col1");
            _store.RegisterIndexes<PersonIndexBothNamesProviderCol>("Col1");

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

                session.Save(bill, "Col1");
                session.Save(elon, "Col1");

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(1, await session.Query<Person, PersonByNameCol>(x => x.Name == "Bill", "Col1").CountAsync());
                Assert.Equal(1, await session.Query<Person, PersonByBothNamesCol>(x => x.Lastname == "Gates", "Col1").CountAsync());

                Assert.Equal(1, await session.Query<Person, PersonByNameCol>(collection: "Col1").Where(x => x.Name.IsIn<PersonByBothNamesCol>(y => y.Firstname, y => y.Lastname.StartsWith("G"))).CountAsync());

                Assert.Equal(0, await session.Query<Person, PersonByNameCol>(collection: "Col1").Where(x => x.Name.IsNotIn<PersonByBothNamesCol>(y => y.Firstname, y => y.Lastname.StartsWith("G") || y.Lastname.StartsWith("M"))).CountAsync());

                Assert.Equal(2, await session.Query<Person, PersonByNameCol>(collection: "Col1").Where(x => x.Name.IsInAny<PersonByBothNamesCol>(y => y.Firstname)).CountAsync());
                Assert.Equal(0, await session.Query<Person, PersonByNameCol>(collection: "Col1").Where(x => x.Name.IsNotInAny<PersonByBothNamesCol>(y => y.Firstname)).CountAsync());

                Assert.Equal(2, await session.Query("Col1").For<Person>().With<PersonByNameCol>().Where(x => x.Name.IsInAny<PersonByBothNamesCol>(y => y.Firstname)).CountAsync());

            }
        }

        [Fact]
        public async Task ShouldGetAndDeletePerCollection()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                };

                session.Save(bill, "Col1");

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>("Col1").FirstOrDefaultAsync();
                Assert.NotNull(person);

                person = await session.GetAsync<Person>(person.Id, "Col1");
                Assert.NotNull(person);

                session.Delete(person, "Col1");

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>("Col1").FirstOrDefaultAsync();
                Assert.Null(person);
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                bill.Firstname = "Bill2";
                session.Save(bill);
                await session.FlushAsync();

                Assert.Equal(1, await session.Query<Person, PersonByName>().CountAsync());
                Assert.Equal(0, await session.QueryIndex<PersonByName>().Where(x => x.SomeName == "Bill").CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>().Where(x => x.SomeName == "Bill2").CountAsync());

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            int result;

            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                var dialect = _store.Configuration.SqlDialect;
                var sql = "SELECT " + dialect.RenderMethod(method, dialect.QuoteForColumnName(nameof(ArticleByPublishedDate.PublishedDateTime))) + " FROM " + dialect.QuoteForTableName(TablePrefix + nameof(ArticleByPublishedDate));
                result = await connection.QueryFirstOrDefaultAsync<int>(sql);
            }

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task SqlNowFunction()
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            var now = DateTime.UtcNow;

            using (var session = _store.CreateSession())
            {
                for (var i = 1; i < 11; i++)
                {
                    session.Save(new Article
                    {
                        PublishedUtc = now.AddDays(i)
                    });
                    session.Save(new Article
                    {
                        PublishedUtc = now.AddDays(-i)
                    });
                }

                await session.SaveChangesAsync();
            }

            int publishedInTheFutureResult, publishedInThePastResult;

            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                var dialect = _store.Configuration.SqlDialect;

                var publishedInTheFutureSql = "SELECT count(1) FROM " + dialect.QuoteForTableName(TablePrefix + nameof(ArticleByPublishedDate)) + " WHERE " + dialect.QuoteForColumnName(nameof(ArticleByPublishedDate.PublishedDateTime)) + " > " + dialect.RenderMethod("now");
                publishedInTheFutureResult = await connection.QueryFirstOrDefaultAsync<int>(publishedInTheFutureSql);

                var publishedInThePastSql = "SELECT count(1) FROM " + dialect.QuoteForTableName(TablePrefix + nameof(ArticleByPublishedDate)) + " WHERE " + dialect.QuoteForColumnName(nameof(ArticleByPublishedDate.PublishedDateTime)) + " < " + dialect.RenderMethod("now");
                publishedInThePastResult = await connection.QueryFirstOrDefaultAsync<int>(publishedInThePastSql);
            }

            Assert.Equal(10, publishedInTheFutureResult);
            Assert.Equal(10, publishedInThePastResult);
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var user = await session.Query<User>().FirstOrDefaultAsync();
                user.RoleNames.Remove("editor");
                session.Save(user);

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var user = await session.Query<User>().FirstOrDefaultAsync();
                user.RoleNames.Add("editor");
                session.Save(user);

                await session.SaveChangesAsync();
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

                for (var i = 0; i < 20; i++)
                {
                    session.Save(new Person { Firstname = $"Foo {i}" });
                }

                await session.SaveChangesAsync();
            }

            var swGated = new Stopwatch();
            var swNonGated = new Stopwatch();
            var gatedCounter = 0;
            var nonGatedCounter = 0;

            var concurrency = 16;
            var MaxTransactions = 50000;

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

            await Task.Delay(TimeSpan.FromSeconds(3));

            // Flushing tasks
            stopping = true;

            await Task.WhenAll(tasks);

            stopping = false;
            counter = 0;

            // Gated queries

            swGated.Restart();

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

                gatedCounter = counter;
                swGated.Stop();
            })).ToList();

            await Task.Delay(TimeSpan.FromSeconds(3));

            // Flushing tasks
            stopping = true;

            await Task.WhenAll(tasks);

            stopping = false;
            counter = 0;

            // Non-gated queries

            _store.Configuration.DisableQueryGating();

            swNonGated.Restart();

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

                nonGatedCounter = counter;
                swNonGated.Stop();
            })).ToList();

            await Task.Delay(TimeSpan.FromSeconds(3));

            // Flushing tasks
            stopping = true;

            await Task.WhenAll(tasks);

            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"{gatedCounter} gated queries in {swGated.Elapsed}");
            Console.WriteLine($"{nonGatedCounter} non-gated in {swNonGated.Elapsed}");
            Console.WriteLine($"Gated: {gatedCounter * 1000 / swGated.ElapsedMilliseconds:n0} tps; NonGated: {nonGatedCounter * 1000 / swNonGated.ElapsedMilliseconds:n0} tps");
            Console.ForegroundColor = previousColor;
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
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
        public async Task ShouldQueryOrderByRandom()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                for (var i = 0; i < 100; i++)
                {
                    session.Save(new Person { Firstname = i < 50 ? "D" : "E" });
                }

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var results = await session.Query<Person, PersonByName>().OrderByRandom().ListAsync();

                var idArray = Enumerable.Range(1, 100).Select(x => Convert.ToInt64(x)).ToArray();
                Assert.NotEqual(idArray, results.Select(x => x.Id));
            }
        }

        [Fact]
        public async Task ShouldQueryThenByRandom()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                for (var i = 0; i < 100; i++)
                {
                    session.Save(new Person { Firstname = i < 50 ? "D" : "E" });
                }

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var results = await session.Query<Person, PersonByName>().OrderBy(x => x.SomeName).ThenByRandom().ListAsync();

                var first50Persons = results.Take(50);
                Assert.All(first50Persons, person => Assert.Equal("D", person.Firstname));
                var idArray = Enumerable.Range(1, 50).Select(x => Convert.ToInt64(x)).ToArray();
                Assert.NotEqual(idArray, first50Persons.Select(x => x.Id));
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

                await session.SaveChangesAsync();
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var all = await session.Query<Person>().ListAsync();
                Assert.Single(all);
                Assert.Equal("Gates", all.First().Lastname);
            }
        }

        [Fact]
        public async Task ShouldHandleNullableFields()
        {
            _store.RegisterIndexes<PersonByNullableAgeIndexProvider>();

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

                var isaac = new Person
                {
                    Firstname = "Isaac",
                    Lastname = "Newton",
                    Age = 376
                };

                session.Save(bill);
                session.Save(elon);
                session.Save(isaac);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(3, await session.Query<Person, PersonByNullableAge>().CountAsync());
                Assert.Equal(1, await session.Query<Person, PersonByNullableAge>(x => x.Age == null).CountAsync());
            }
        }

        [Fact]
        public async Task ShouldLogSql()
        {
            var logger = new TestLogger();

            _store.Configuration.Logger = logger;

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Age = 50
                };

                session.Save(bill);

                await session.SaveChangesAsync();
            }

            Assert.NotEmpty(logger.ToString());
        }

        [Fact]
        public async Task ShouldNotGenerateDeleteSatementsForFilteredIndexProviders()
        {
            var logger = new TestLogger();

            _store.Configuration.Logger = logger;

            _store.RegisterIndexes<PersonIndexProvider>();
            _store.RegisterIndexes<PersonAgeIndexFilterProvider1>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Person { Firstname = "A" });

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var results = await session.Query<Person, PersonByName>().ListAsync();
                Assert.Equal("A", results.ElementAt(0).Firstname);
                session.Delete(results.First());

                await session.SaveChangesAsync();
            }

            Assert.NotEmpty(logger.ToString());
            Assert.DoesNotContain("PersonByAge", logger.ToString());
        }

        [Fact]
        public async Task ShouldGenerateDeleteSatementsForFilteredIndexProviders()
        {
            var logger = new TestLogger();

            _store.Configuration.Logger = logger;

            _store.RegisterIndexes<PersonIndexProvider>();
            _store.RegisterIndexes<PersonAgeIndexFilterProvider2>();

            using (var session = _store.CreateSession())
            {
                session.Save(new Person { Firstname = "A" });

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var results = await session.Query<Person, PersonByName>().ListAsync();
                Assert.Equal("A", results.ElementAt(0).Firstname);
                session.Delete(results.First());

                await session.SaveChangesAsync();
            }

            Assert.NotEmpty(logger.ToString());
            Assert.Contains("PersonByAge", logger.ToString());
        }

        [Fact]
        public async Task ShouldCreateMoreObjectThanIdBlock()
        {
            var lastId = 0L;
            var firstId = 0L;

            using (var session = _store.CreateSession())
            {
                for (var k = 0; k < 1000; k++)
                {
                    var person = new Person { Firstname = "Bill" };
                    session.Save(person);
                    lastId = person.Id;

                    if (firstId == 0)
                    {
                        firstId = person.Id;
                    }
                }

                await session.SaveChangesAsync();
            }

            Assert.Equal(firstId + 1000 - 1, lastId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Col1")]
        public async Task ShouldGenerateIdsConcurrently(string collection)
        {
            var cts = new CancellationTokenSource(10000);
            var concurrency = 5;
            var man = new ManualResetEventSlim();
            var MaxTransactions = 1000;
            long lastId = 0;
            var results = new bool[MaxTransactions + concurrency];

            var tasks = Enumerable.Range(1, concurrency).Select(i => Task.Run(() =>
            {
                long taskId;
                man.Wait();

                while (!cts.IsCancellationRequested)
                {
                    lastId = taskId = _store.Configuration.IdGenerator.GetNextId(collection);

                    if (taskId > MaxTransactions)
                    {
                        break;
                    }

                    Assert.False(results[taskId], $"Found duplicate identifier: '{taskId}'");
                    results[taskId] = true;

                    System.Diagnostics.Debug.WriteLine($"{i}:{taskId}");
                }
            })).ToList();

            await Task.Delay(1000);
            man.Set();
            await Task.WhenAll(tasks);

            Assert.True(lastId >= MaxTransactions, $"lastId: {lastId}");
        }

        [Fact]
        public async Task ShouldNotDuplicateCommandsWhenCommitFails()
        {

            using (var session = (Session)_store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                session.Save(bill);

                // Create a command that will throw an exception
                // Use an order of 4 so it will be at the end of the list
                session._commands = new List<IIndexCommand>();
                session._commands.Add(new FailingCommand(new Document()));

                await Assert.ThrowsAnyAsync<Exception>(async () => await session.SaveChangesAsync());

                Assert.Null(session._commands);
            }
        }

        [Fact]
        public async Task ShouldResolveManyTypes()
        {
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                var lion = new Animal
                {
                    Name = "Lion"
                };

                session.Save(bill);
                session.Save(lion);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var all = await session.Query().Any().ListAsync();

                Assert.Equal(2, all.Count());
                Assert.Contains(all, x => x is Person);
                Assert.Contains(all, x => x is Animal);
            }
        }

        [Fact]
        public async Task ShouldReturnSingleDocument()
        {
            _store.RegisterIndexes<EmailByAttachmentProvider>();

            using (var session = _store.CreateSession())
            {
                var email = new Email()
                {
                    Date = DateTime.Now,
                    Attachments = new List<Attachment>()
                    {
                        new Attachment("resume.doc"),
                        new Attachment("letter.doc"),
                        new Attachment("photo.jpg")
                    }
                };

                session.Save(email);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                // Get all emails that have '.doc' attachments
                var result = await session.Query<Email, EmailByAttachment>().Where(e => e.AttachmentName.EndsWith(".doc")).ListAsync();

                Assert.Single(result);
            }
        }

        [Fact]
        public async Task ShouldCountSingleDocument()
        {
            _store.RegisterIndexes<EmailByAttachmentProvider>();

            using (var session = _store.CreateSession())
            {
                var email = new Email()
                {
                    Date = DateTime.Now,
                    Attachments = new List<Attachment>()
                    {
                        new Attachment("resume.doc"),
                        new Attachment("letter.doc"),
                        new Attachment("photo.jpg")
                    }
                };

                session.Save(email);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                // Get all emails that have '.doc' attachments
                var count = await session.Query<Email, EmailByAttachment>().Where(e => e.AttachmentName.EndsWith(".doc")).CountAsync();

                Assert.Equal(1, count);
            }
        }

        [Fact]
        public virtual void ShouldRenameColumn()
        {
            var table = "Table1";
            var prefixedTable = TablePrefix + table;
            var column1 = "Column1";
            var column2 = "Column2";
            var value = "Value";

            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                try
                {
                    using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                    {

                        var builder = new SchemaBuilder(_store.Configuration, transaction);

                        builder.DropTable(table);

                        transaction.Commit();
                    }
                }
                catch
                {
                    // Do nothing if the table can't be dropped
                }

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {

                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.CreateTable(table, column => column
                            .Column<string>(column1)
                        );

                    var sqlInsert = String.Format("INSERT INTO {0} ({1}) VALUES({2})",
                        _store.Configuration.SqlDialect.QuoteForTableName(prefixedTable),
                        _store.Configuration.SqlDialect.QuoteForColumnName(column1),
                        _store.Configuration.SqlDialect.GetSqlValue(value)
                        );

                    connection.Execute(sqlInsert, transaction: transaction);

                    transaction.Commit();
                }

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var sqlSelect = String.Format("SELECT {0} FROM {1}",
                        _store.Configuration.SqlDialect.QuoteForColumnName(column1),
                        _store.Configuration.SqlDialect.QuoteForTableName(prefixedTable)
                        );

                    var result = connection.Query(sqlSelect, transaction: transaction).FirstOrDefault();

                    Assert.Equal(value, result.Column1);

                    transaction.Commit();
                }

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.AlterTable(table, column => column
                            .RenameColumn(column1, column2)
                        );

                    transaction.Commit();
                }

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var sqlSelect = String.Format("SELECT {0} FROM {1}",
                        _store.Configuration.SqlDialect.QuoteForColumnName(column2),
                        _store.Configuration.SqlDialect.QuoteForTableName(prefixedTable)
                        );

                    var result = connection.Query(sqlSelect, transaction: transaction).FirstOrDefault();

                    Assert.Equal(value, result.Column2);

                    transaction.Commit();
                }

            }
        }

        [Fact]
        public virtual async Task ShouldHandleConcurrency()
        {
            using (var session = _store.CreateSession())
            {
                var email = new Person { Firstname = "Bill" };

                session.Save(email);

                await session.SaveChangesAsync();
            }

            var task1Saved = new ManualResetEvent(false);
            var task2Loaded = new ManualResetEvent(false);
            var task1Loaded = new ManualResetEvent(false);

            var task1 = Task.Run(async () =>
            {
                using (var session = _store.CreateSession())
                {
                    var person = await session.Query<Person>().FirstOrDefaultAsync();
                    Assert.NotNull(person);

                    task1Loaded.Set();

                    // Wait for the other thread to load the person before updating it
                    if (!task2Loaded.WaitOne(5000))
                    {
                        Assert.True(false, "task2Loaded timeout");
                        await session.CancelAsync();
                    }

                    person.Lastname = "Gates";

                    session.Save(person, true);
                    Assert.NotNull(person);

                    await session.SaveChangesAsync();
                }

                // Noify the other thread to save
                task1Saved.Set();
            });

            var task2 = Task.Run(async () =>
            {
                task1Loaded.WaitOne(5000);

                await Assert.ThrowsAsync<ConcurrencyException>(async () =>
                {
                    using (var session = _store.CreateSession())
                    {
                        var person = await session.Query<Person>().FirstOrDefaultAsync();
                        Assert.NotNull(person);

                        task2Loaded.Set();

                        // Wait for the other thread to save the person before updating it
                        if (!task1Saved.WaitOne(5000))
                        {
                            Assert.True(false, "task1Saved timeout");
                            await session.CancelAsync();
                        }

                        person.Lastname = "Doors";
                        session.Save(person, true);
                        Assert.NotNull(person);

                        await session.SaveChangesAsync();
                    }
                });
            });

            await Task.WhenAll(task1, task2);

            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);
                Assert.Equal("Gates", person.Lastname);
            }
        }

        [Fact]
        public virtual async Task ShouldNotThrowConcurrencyException()
        {
            _store.Configuration.CheckConcurrentUpdates<Person>();

            using (var session = _store.CreateSession())
            {
                var email = new Person { Firstname = "Bill" };

                session.Save(email);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                person.Lastname = "Gates";

                session.Save(person);
                Assert.NotNull(person);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                person.Lastname = "Doors";
                session.Save(person);
                Assert.NotNull(person);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);
                Assert.Equal("Doors", person.Lastname);
            }
        }

        [Fact]
        public async Task ShouldReloadDetachedObject()
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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                bill = new Person
                {
                    Id = bill.Id,
                    Firstname = "Bill",
                    Lastname = "Gates",
                };

                session.Save(bill);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var all = await session.Query<Person>().ListAsync();
                Assert.Single(all);
                Assert.Equal("Gates", all.First().Lastname);
            }
        }

        [Fact]
        public async Task ShouldDetachEntity()
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

                Assert.Equal(bill, newBill);

                session.Detach(bill);

                newBill = await session.GetAsync<Person>(bill.Id);

                Assert.NotEqual(bill, newBill);

                await session.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task ShouldStoreBinaryInIndex()
        {
            _store.RegisterIndexes<BinaryIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person { Firstname = "Bill" };
                session.Save(bill);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var binary = await session.QueryIndex<Binary>().FirstOrDefaultAsync();

                Assert.NotNull(binary);
                Assert.Equal(255, binary.Content1.Length);
                Assert.Equal(8000, binary.Content2.Length);
                Assert.Equal(65535, binary.Content3.Length);
                Assert.Null(binary.Content4);
            }
        }

        [Fact]
        public async Task ShouldCreateAndIndexPropertyWithMaximumKeyLengths()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder
                        .DropMapIndexTable<PropertyIndex>();

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                        .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(767))
                        .Column<bool>(nameof(PropertyIndex.ForRent))
                        .Column<bool>(nameof(PropertyIndex.IsOccupied))
                        .Column<string>(nameof(PropertyIndex.Location))
                        );

                    builder
                        .AlterTable(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name", "ForRent", "IsOccupied"));

                    transaction.Commit();
                }
            }

            _store.RegisterIndexes<PropertyIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var property = new Property
                {
                    Name = new string('*', 767),
                    IsOccupied = true,
                    ForRent = true
                };

                session.Save(property);
            }
        }

        [Fact]
        public async Task ShouldCommitInMultipleCollections()
        {
            using (var session = _store.CreateSession())
            {
                var steve = new
                {
                    Firstname = "Steve",
                    Lastname = "Balmer"
                };

                session.Save(steve);

                var bill = new
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };


                session.Save(bill, "Col1");

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var count = await session.Query("Col1").Any().CountAsync();
                Assert.Equal(1, count);

                count = await session.Query().Any().CountAsync();
                Assert.Equal(1, count);
            }
        }

        [Fact]
        public async Task ShouldStoreCollectionIndexesInDistinctTables()
        {
            _store.RegisterIndexes<PersonIndexProvider>("Col1");

            using (var session = _store.CreateSession())
            {
                var steve = new Person
                {
                    Firstname = "Steve",
                    Lastname = "Balmer"
                };

                session.Save(steve);

                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates"
                };

                session.Save(bill, "Col1");

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                Assert.Equal(0, await session.QueryIndex<PersonByName>().CountAsync());
                Assert.Equal(1, await session.QueryIndex<PersonByName>("Col1").CountAsync());
            }
        }

        [Fact]
        public virtual async Task ShouldSetVersionProperty()
        {
            _store.Configuration.CheckConcurrentUpdates<Person>();

            using (var session = _store.CreateSession())
            {
                var doc1 = new Person { Firstname = "Bill", Version = 11 };

                session.Save(doc1);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var doc1 = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(doc1);

                Assert.Equal(11, doc1.Version);
            }
        }

        [Fact]
        public virtual async Task ShouldIncrementVersionProperty()
        {
            _store.Configuration.CheckConcurrentUpdates<Person>();

            using (var session = _store.CreateSession())
            {
                var doc1 = new Person { Firstname = "Bill" };

                session.Save(doc1);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var doc1 = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(doc1);

                Assert.Equal(1, doc1.Version);
            }
        }

        [Fact]
        public virtual async Task ShouldCheckVersionHaschanged()
        {
            // Simulates a long running workflow where a web page
            // wants to update a document, and check that the state it
            // stored was not updated in the meantime.
            // The difference with other concurrency checks is that 
            // the Controller would reload a Document and apply changes,
            // but the Version would not detect the concurrent conflict
            // as the controller has reloaded the document with the new version.

            // In this test the version is checked against the view model 
            // to ensure we are modifying the correct version

            _store.Configuration.CheckConcurrentUpdates<Person>();

            // Create initial document
            using (var session = _store.CreateSession())
            {
                var email = new Person { Firstname = "Bill" };

                session.Save(email);

                await session.SaveChangesAsync();
            }

            var viewModel = new Person
            {
                Firstname = "",
                Version = 0
            };

            // User A loads the document, stores in a view model
            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                // The object is new, its version should be 1
                Assert.Equal(1, person.Version);

                viewModel.Firstname = person.Firstname;
                viewModel.Version = person.Version;

                await session.SaveChangesAsync();
            }

            // User B loads the document, updates it
            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                person.Firstname = "William";

                session.Save(person);

                // The object is new, its version should be 1
                Assert.Equal(1, person.Version);

                await session.SaveChangesAsync();
            }

            // User A submits the changes, and should detect the version has changed
            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                Assert.NotEqual(person.Version, viewModel.Version);
            }
        }

        [Fact]
        public virtual async Task ShouldDetectVersionHaschanged()
        {
            // Simulates a long running workflow where a web page
            // wants to update a document, and check that the state it
            // stored was not updated in the meantime.
            // The difference with other concurrency checks is that 
            // the Controller would reload a Document and apply changes,
            // but the Version would not detect the concurrent conflict
            // as the controller has reloaded the document with the new version.

            // In this test the stored version is assigned to the loaded object
            // and a ConcurrencyException is thrown

            _store.Configuration.CheckConcurrentUpdates<Person>();

            // Create initial document
            using (var session = _store.CreateSession())
            {
                var email = new Person { Firstname = "Bill" };

                session.Save(email);

                await session.SaveChangesAsync();
            }

            var viewModel = new Person
            {
                Firstname = "",
                Version = 0
            };

            // User A loads the document, stores in a view model
            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                // The object is new, its version should be 1
                Assert.Equal(1, person.Version);

                viewModel.Firstname = person.Firstname;
                viewModel.Version = person.Version;

                await session.SaveChangesAsync();
            }

            // User B loads the document, updates it
            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                person.Firstname = "William";

                session.Save(person);

                // The object is new, its version should be 1
                Assert.Equal(1, person.Version);

                await session.SaveChangesAsync();
            }

            // User A submits the changes
            await Assert.ThrowsAsync<ConcurrencyException>(async () =>
            {
                using (var session = _store.CreateSession())
                {
                    var person = await session.Query<Person>().FirstOrDefaultAsync();
                    Assert.NotNull(person);

                    person.Version = viewModel.Version;
                    person.Firstname = viewModel.Firstname;

                    session.Save(person);

                    await session.SaveChangesAsync();
                }
            });

            // Changes should not have been persisted
            using (var session = _store.CreateSession())
            {
                var person = await session.Query<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                Assert.Equal("William", person.Firstname);
            }
        }

        [Theory]
        [ClassData(typeof(DecimalPrecisionAndScaleDataGenerator))]
        public void SqlDecimalPrecisionAndScale(byte? precision, byte? scale)
        {
            string expected = string.Format(DecimalColumnDefinitionFormatString, precision ?? _store.Configuration.SqlDialect.DefaultDecimalPrecision, scale ?? _store.Configuration.SqlDialect.DefaultDecimalScale);

            string result = _store.Configuration.SqlDialect.GetTypeName(DbType.Decimal, null, precision, scale);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task ShouldUpdateVersions()
        {
            // c.f. https://github.com/sebastienros/yessql/pull/287

            _store.Configuration.CheckConcurrentUpdates<Car>();

            using (var session = _store.CreateSession())
            {
                var c = new Car { Name = "Clio" };

                session.Save(c);

                await session.SaveChangesAsync();
            }

            // Create initial document
            using (var session = _store.CreateSession())
            {
                // Load the existing car
                var c = await session.Query<Car>().FirstOrDefaultAsync();

                session.Save(c);

                await session.Query<Person>().FirstOrDefaultAsync();

                await session.Query<Person>().FirstOrDefaultAsync();

                c.Name = "Clio 2";
            }
        }

        [Fact]
        public async Task AllDataTypesShouldBeStored()
        {
            var dummy = new Person();

            var valueTimeSpan = new TimeSpan(1, 2, 3, 4, 5);
            var valueDateTime = new DateTime(2021, 1, 20);
            var valueGuid = Guid.Parse("cf0ef7ac-b6fe-4e24-aeda-a2b45bb5654e");
            var valueBool = false;
            var valueDateTimeOffset = new DateTimeOffset(valueDateTime, new TimeSpan(1, 2, 0));

            // Create fake document to associate to index
            using (var session = _store.CreateSession())
            {
                session.Save(dummy);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var index = new TypesIndex();

                index.ValueDateTime = valueDateTime;
                index.ValueGuid = valueGuid;
                index.ValueBool = valueBool;
                index.ValueDateTimeOffset = valueDateTimeOffset;
                index.ValueTimeSpan = valueTimeSpan;

                ((IIndex)index).AddDocument(new Document { Id = dummy.Id });

                var connection = await session.CreateConnectionAsync();
                var transaction = await session.BeginTransactionAsync();

                await new CreateIndexCommand(index, new[] { dummy.Id }, session.Store, "").ExecuteAsync(connection, transaction, session.Store.Configuration.SqlDialect, session.Store.Configuration.Logger);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var index = await session.QueryIndex<TypesIndex>().FirstOrDefaultAsync();

                Assert.Equal(valueDateTime, index.ValueDateTime);
                Assert.Equal(valueGuid, index.ValueGuid);
                Assert.Equal(valueBool, index.ValueBool);
                Assert.Equal(valueDateTimeOffset, index.ValueDateTimeOffset);
                Assert.Equal(valueTimeSpan, index.ValueTimeSpan);

                Assert.Equal(0, index.ValueDecimal);
                Assert.Equal(0, index.ValueDouble);
                Assert.Equal(0, index.ValueFloat);
                Assert.Equal(0, index.ValueInt);
                Assert.Equal(0, index.ValueLong);
                Assert.Equal(0, index.ValueShort);
            }
        }

        [Fact]
        public async Task AllDataTypesShouldBeQueryableWithProperties()
        {
            var dummy = new Person();

            var valueTimeSpan = new TimeSpan(1, 2, 3, 4, 5);
            var valueDateTime = new DateTime(2021, 1, 20);
            var valueGuid = Guid.Parse("cf0ef7ac-b6fe-4e24-aeda-a2b45bb5654e");
            var valueBool = false;
            var valueDateTimeOffset = new DateTimeOffset(valueDateTime, new TimeSpan(1, 2, 0));

            // Create fake document to associate to index
            using (var session = _store.CreateSession())
            {
                session.Save(dummy);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var index = new TypesIndex();

                index.ValueDateTime = valueDateTime;
                index.ValueGuid = valueGuid;
                index.ValueBool = valueBool;
                index.ValueDateTimeOffset = valueDateTimeOffset;
                index.ValueTimeSpan = valueTimeSpan;

                ((IIndex)index).AddDocument(new Document { Id = dummy.Id });

                var connection = await session.CreateConnectionAsync();
                var transaction = await session.BeginTransactionAsync();

                await new CreateIndexCommand(index, new[] { dummy.Id }, session.Store, "").ExecuteAsync(connection, transaction, session.Store.Configuration.SqlDialect, session.Store.Configuration.Logger);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                // Ensure that query builing is also converting the values
                var index = await session.QueryIndex<TypesIndex>(x =>
                x.ValueBool == valueBool
                && x.ValueDateTime == valueDateTime
                && x.ValueDateTimeOffset == valueDateTimeOffset
                && x.ValueTimeSpan == valueTimeSpan
                && x.ValueGuid == valueGuid).FirstOrDefaultAsync();

                Assert.Equal(valueDateTime, index.ValueDateTime);
                Assert.Equal(valueGuid, index.ValueGuid);
                Assert.Equal(valueBool, index.ValueBool);
                Assert.Equal(valueDateTimeOffset, index.ValueDateTimeOffset);
                Assert.Equal(valueTimeSpan, index.ValueTimeSpan);

                Assert.Equal(0, index.ValueDecimal);
                Assert.Equal(0, index.ValueDouble);
                Assert.Equal(0, index.ValueFloat);
                Assert.Equal(0, index.ValueInt);
                Assert.Equal(0, index.ValueLong);
                Assert.Equal(0, index.ValueShort);
            }
        }

        [Fact]
        public async Task AllDataTypesShouldBeQueryableWithConstants()
        {
            var dummy = new Person();

            var valueTimeSpan = new TimeSpan(1, 2, 3, 4, 5);
            var valueDateTime = new DateTime(2021, 1, 20);
            var valueGuid = Guid.Parse("cf0ef7ac-b6fe-4e24-aeda-a2b45bb5654e");
            var valueBool = false;
            var valueDateTimeOffset = new DateTimeOffset(valueDateTime, new TimeSpan(1, 2, 0));

            // Create fake document to associate to index
            using (var session = _store.CreateSession())
            {
                session.Save(dummy);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var index = new TypesIndex();

                index.ValueDateTime = valueDateTime;
                index.ValueGuid = valueGuid;
                index.ValueBool = valueBool;
                index.ValueDateTimeOffset = valueDateTimeOffset;
                index.ValueTimeSpan = valueTimeSpan;

                ((IIndex)index).AddDocument(new Document { Id = dummy.Id });

                var connection = await session.CreateConnectionAsync();
                var transaction = await session.BeginTransactionAsync();

                await new CreateIndexCommand(index, new[] { dummy.Id }, session.Store, "").ExecuteAsync(connection, transaction, session.Store.Configuration.SqlDialect, session.Store.Configuration.Logger);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                // Ensure that query builing is also converting constants
                var index = await session.QueryIndex<TypesIndex>(x =>
                    x.ValueBool == false
                    && x.ValueDateTime == new DateTime(2021, 1, 20)
                    && x.ValueDateTimeOffset == new DateTimeOffset(new DateTime(2021, 1, 20), new TimeSpan(1, 2, 0))
                    && x.ValueTimeSpan == new TimeSpan(1, 2, 3, 4, 5)
                    && x.ValueGuid == Guid.Parse("cf0ef7ac-b6fe-4e24-aeda-a2b45bb5654e")
                ).FirstOrDefaultAsync();

                Assert.Equal(valueDateTime, index.ValueDateTime);
                Assert.Equal(valueGuid, index.ValueGuid);
                Assert.Equal(valueBool, index.ValueBool);
                Assert.Equal(valueDateTimeOffset, index.ValueDateTimeOffset);
                Assert.Equal(valueTimeSpan, index.ValueTimeSpan);

                Assert.Equal(0, index.ValueDecimal);
                Assert.Equal(0, index.ValueDouble);
                Assert.Equal(0, index.ValueFloat);
                Assert.Equal(0, index.ValueInt);
                Assert.Equal(0, index.ValueLong);
                Assert.Equal(0, index.ValueShort);
            }
        }

        [Fact]
        public async Task NullValuesShouldBeStoredInNullableFields()
        {
            var dummy = new Person();

            // Create fake document to associate to index
            using (var session = _store.CreateSession())
            {
                session.Save(dummy);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var index = new TypesIndex
                {
                    ValueTimeSpan = new TimeSpan(1, 2, 3, 4, 5),
                    ValueDateTime = new DateTime(2021, 1, 20),
                    ValueGuid = Guid.Parse("cf0ef7ac-b6fe-4e24-aeda-a2b45bb5654e"),
                    ValueDateTimeOffset = new DateTimeOffset(new DateTime(2021, 1, 20), new TimeSpan(1, 2, 0))
                };

                ((IIndex)index).AddDocument(new Document { Id = dummy.Id });

                var connection = await session.CreateConnectionAsync();
                var transaction = await session.BeginTransactionAsync();

                await new CreateIndexCommand(index, new[] { dummy.Id }, session.Store, "").ExecuteAsync(connection, transaction, session.Store.Configuration.SqlDialect, session.Store.Configuration.Logger);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var index = await session.QueryIndex<TypesIndex>().FirstOrDefaultAsync();

                Assert.Null(index.NullableBool);
                Assert.Null(index.NullableDateTime);
                Assert.Null(index.NullableDecimal);
                Assert.Null(index.NullableDouble);
                Assert.Null(index.NullableFloat);
                Assert.Null(index.NullableGuid);
                Assert.Null(index.NullableInt);
                Assert.Null(index.NullableLong);
                Assert.Null(index.NullableShort);
                Assert.Null(index.NullableDateTimeOffset);
                Assert.Null(index.NullableTimeSpan);
            }
        }

        [Fact]
        public async Task ShouldSplitBatchCommandsByMaxParameters()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();
            _store.Configuration.CommandsPageSize = int.MaxValue;

            using (var session = _store.CreateSession())
            {
                // This will produce 2000 queries (1 doc + 1 index) and 8000 parameters (4/doc and 4/index)
                for (var i = 0; i < 1000; i++)
                {
                    session.Save(new Person { Age = i, Firstname = i.ToString(), Lastname = i.ToString() });
                }

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var persons = (await session.Query<Person>().ListAsync());
                Assert.Equal(1000, persons.Count());
                // This will produce a query that batches at 2099 parameters which will fail.
                // When reduced to a maximum to 2098, i.e. stopping the batch before 2099, it will pass.
                foreach (var person in persons)
                {
                    session.Save(person);
                }
            }
        }

        [Fact]
        public async Task SameQueryShouldBeReusable()
        {
            // ListAsync() and FirstDefaultAsync() can be called after CountAsync()
            // ListAsync() and FirstDefaultAsync() can't be called one after the other

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

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var queryIndex = session.QueryIndex<PersonByName>(x => x.SomeName == "Bill");

                Assert.Equal(1, await queryIndex.CountAsync());
                Assert.Single(await queryIndex.ListAsync());

                Assert.Equal(1, await queryIndex.CountAsync());
                Assert.NotNull(await queryIndex.FirstOrDefaultAsync());
            }
        }

        #region FilterTests

        [Fact]
        public async Task ShouldParseNamedTermQuery()
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
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

                session.Save(billsArticle);
                session.Save(stevesArticle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var filter = "title:steve";

                var filterQuery = session.Query<Article>();

                var parser = new QueryEngineBuilder<Article>()
                    .WithNamedTerm("title", b => b
                        .OneCondition((val, query) => query.With<ArticleByPublishedDate>(x => x.Title.Contains(val)))
                    )
                    .Build();

                var parsed = parser.Parse(filter);

                await parsed.ExecuteAsync(filterQuery);

                // Normal YesSql query
                Assert.Equal("post by steve about cats", (await session.Query().For<Article>().With<ArticleByPublishedDate>(x => x.Title.Contains("steve")).FirstOrDefaultAsync()).Title);
                Assert.Equal(1, await session.Query().For<Article>().With<ArticleByPublishedDate>(x => x.Title.Contains("steve")).CountAsync());

                // Parsed query
                Assert.Equal("post by steve about cats", (await filterQuery.FirstOrDefaultAsync()).Title);
                Assert.Equal(1, await filterQuery.CountAsync());
            }
        }

        [Theory]
        [InlineData("steve")]
        [InlineData("title:steve")]
        public async Task ShouldParseDefaultTermQuery(string search)
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
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

                session.Save(billsArticle);
                session.Save(stevesArticle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var filterQuery = session.Query<Article>();

                var parser = new QueryEngineBuilder<Article>()
                    .WithDefaultTerm("title", b => b
                        .OneCondition((val, query) => query.With<ArticleByPublishedDate>(x => x.Title.Contains(val))))
                    .Build();

                var parsed = parser.Parse(search);

                await parsed.ExecuteAsync(filterQuery);

                // Normal YesSql query
                Assert.Equal("post by steve about cats", (await session.Query().For<Article>().With<ArticleByPublishedDate>(x => x.Title.Contains("steve")).FirstOrDefaultAsync()).Title);
                Assert.Equal(1, await session.Query().For<Article>().With<ArticleByPublishedDate>(x => x.Title.Contains("steve")).CountAsync());

                // Parsed query
                Assert.Equal("post by steve about cats", (await filterQuery.FirstOrDefaultAsync()).Title);
                Assert.Equal(1, await filterQuery.CountAsync());
            }
        }

        [Fact]
        public async Task ShouldParseOrQuery()
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
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

                session.Save(billsArticle);
                session.Save(stevesArticle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                // boolean OR "title:(bill OR post)"
                var filter = "title:bill post";
                var filterQuery = session.Query<Article>();

                var parser = new QueryEngineBuilder<Article>()
                    .WithNamedTerm("title", b => b
                        .ManyCondition(
                            (val, query) => query.With<ArticleByPublishedDate>(x => x.Title.Contains(val)),
                            (val, query) => query.With<ArticleByPublishedDate>(x => x.Title.IsNotIn<ArticleByPublishedDate>(s => s.Title, w => w.Title.Contains(val)))
                        )
                    )
                    .Build();

                var parsed = parser.Parse(filter);
                await parsed.ExecuteAsync(filterQuery);

                var yesqlQuery = session.Query().For<Article>()
                    .Any(
                        x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("bill")),
                        x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("post"))
                    );

                // Normal YesSql query
                Assert.Equal(2, await yesqlQuery.CountAsync());

                // Parsed query
                Assert.Equal(2, await filterQuery.CountAsync());
            }
        }

        [Fact]
        public async Task ShouldParseAndQuery()
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
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

                session.Save(billsArticle);
                session.Save(stevesArticle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                // boolean AND "title:(bill AND rabbits)"
                var filter = "title:bill AND rabbits";
                var filterQuery = session.Query<Article>();

                var parser = new QueryEngineBuilder<Article>()
                    .WithNamedTerm("title", b => b
                        .ManyCondition(
                            (val, query) => query.With<ArticleByPublishedDate>(x => x.Title.Contains(val)),
                            (val, query) => query.With<ArticleByPublishedDate>(x => x.Title.IsNotIn<ArticleByPublishedDate>(s => s.Title, w => w.Title.Contains(val)))
                        )
                    )
                    .Build();

                var parsed = parser.Parse(filter);

                await parsed.ExecuteAsync(filterQuery);

                var yesSqlQuery = session.Query().For<Article>()
                    .All(
                        x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("bill")),
                        x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("rabbits"))
                    );

                // Normal YesSql query
                Assert.Equal(1, await yesSqlQuery.CountAsync());

                // Parsed query
                Assert.Equal(1, await filterQuery.CountAsync());
            }
        }

        [Fact]
        public async Task ShouldParseTwoNamedTermQuerys()
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
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

                session.Save(billsArticle);
                session.Save(stevesArticle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var filter = "title:article title:article";
                var filterQuery = session.Query<Article>();

                var parser = new QueryEngineBuilder<Article>()
                    .WithNamedTerm("title", b => b
                        .OneCondition((val, query) => query.With<ArticleByPublishedDate>(x => x.Title.Contains(val)))
                        .AllowMultiple()
                    )
                    .Build();

                var parsed = parser.Parse(filter);

                await parsed.ExecuteAsync(filterQuery);

                var yesSqlQuery = session.Query().For<Article>()
                    .All(
                        x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("article"))
                    )
                    .All(
                        x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("article"))
                    );

                // Normal YesSql query
                Assert.Equal(2, await yesSqlQuery.CountAsync());

                // Parsed query
                Assert.Equal(2, await filterQuery.CountAsync());
            }
        }

        [Fact]
        public async Task ShouldParseComplexQuery()
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
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

                session.Save(beachLizardsArticle);
                session.Save(mountainArticle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var filter = "title:(beach AND sand) OR (mountain AND lake)";
                var filterQuery = session.Query<Article>();

                var parser = new QueryEngineBuilder<Article>()
                    .WithNamedTerm("title", b => b
                        .ManyCondition(
                            (val, query) => query.With<ArticleByPublishedDate>(x => x.Title.Contains(val)),
                            (val, query) => query.With<ArticleByPublishedDate>(x => x.Title.IsNotIn<ArticleByPublishedDate>(s => s.Title, w => w.Title.Contains(val)))
                        )
                    )
                    .Build();

                var parsed = parser.Parse(filter);

                await parsed.ExecuteAsync(filterQuery);

                var yesSqlQuery = session.Query().For<Article>()
                    .Any(
                        x => x.All(
                            x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("beach")),
                            x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("sand"))
                        ),
                        x => x.All(
                            x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("mountain")),
                            x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("lake"))
                        )
                    );

                // Normal YesSql query
                Assert.Equal(2, await yesSqlQuery.CountAsync());

                // Parsed query
                Assert.Equal(2, await filterQuery.CountAsync());
            }
        }

        [Fact]
        public async Task ShouldParseNotComplexQuery()
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
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

                session.Save(beachLizardsArticle);
                session.Save(sandcastlesArticle);
                session.Save(mountainArticle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                // boolean : ((beach AND sand) OR (mountain AND lake)) NOT lizards 
                var filter = "title:((beach AND sand) OR (mountain AND lake)) NOT lizards";
                var filterQuery = session.Query<Article>();

                var parser = new QueryEngineBuilder<Article>()
                    .WithNamedTerm("title", b => b
                        .ManyCondition(
                            (val, query) => query.With<ArticleByPublishedDate>(x => x.Title.Contains(val)),
                            (val, query) => query.With<ArticleByPublishedDate>(x => x.Title.IsNotIn<ArticleByPublishedDate>(s => s.Title, w => w.Title.Contains(val)))
                        )
                    )
                    .Build();

                var parsed = parser.Parse(filter);

                await parsed.ExecuteAsync(filterQuery);

                var yesSqlQuery = session.Query().For<Article>()
                    .Any(
                        x => x.All(
                            x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("beach")),
                            x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("sand"))
                        ),
                        x => x.All(
                            x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("mountain")),
                            x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("lake"))
                        )
                    )
                    .All(
                        x => x.With<ArticleByPublishedDate>(x => x.Title.IsNotIn<ArticleByPublishedDate>(s => s.Title, w => w.Title.Contains("lizards")))
                    );

                // Normal YesSql query
                Assert.Equal(2, await yesSqlQuery.CountAsync());

                // Parsed query
                Assert.Equal(2, await filterQuery.CountAsync());
            }
        }

        [Fact]
        public async Task ShouldParseNotBooleanQuery()
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
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

                session.Save(billsArticle);
                session.Save(stevesArticle);
                session.Save(paulsArticle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var filter = "title:NOT steve";
                var filterQuery = session.Query<Article>();

                var parser = new QueryEngineBuilder<Article>()
                    .WithNamedTerm("title", b => b
                        .ManyCondition(
                            (val, query) => query.With<ArticleByPublishedDate>(x => x.Title.Contains(val)),
                            (val, query) => query.With<ArticleByPublishedDate>(x => x.Title.IsNotIn<ArticleByPublishedDate>(s => s.Title, w => w.Title.Contains(val)))
                        )
                    )
                    .Build();

                var parsed = parser.Parse(filter);

                await parsed.ExecuteAsync(filterQuery);

                var yesSqlQuery = session.Query().For<Article>()
                    .All(
                        x => x.With<ArticleByPublishedDate>(x => x.Title.IsNotIn<ArticleByPublishedDate>(s => s.Title, w => w.Title.Contains("steve")))
                    )
                    ;

                // Normal YesSql query
                Assert.Equal(2, await yesSqlQuery.CountAsync());

                // Parsed query
                Assert.Equal(2, await filterQuery.CountAsync());
            }
        }

        [Fact]
        public async Task ShouldParseNotQueryWithOrder()
        {
            _store.RegisterIndexes<ArticleBydPublishedDateProvider>();

            using (var session = _store.CreateSession())
            {
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

                session.Save(billsArticle);
                session.Save(stevesArticle);
                session.Save(paulsArticle);

                await session.SaveChangesAsync();
            }

            using (var session = _store.CreateSession())
            {
                var filter = "title:about NOT steve";
                var filterQuery = session.Query<Article>();

                var parser = new QueryEngineBuilder<Article>()
                    .WithNamedTerm("title", b => b
                        .ManyCondition(
                            (val, query) => query.With<ArticleByPublishedDate>(x => x.Title.Contains(val)),
                            (val, query) => query
                                .With<ArticleByPublishedDate>(x => x.Title.IsNotIn<ArticleByPublishedDate>(s => s.Title, w => w.Title.Contains(val)))
                                .OrderByDescending(x => x.Title)
                        )
                    )
                    .Build();

                var parsed = parser.Parse(filter);

                await parsed.ExecuteAsync(filterQuery);

                // Order queries can be placed anywhere inside the booleans and they still get processed fine.  
                var yesSqlQuery = session.Query().For<Article>()
                    .All(
                        x => x.With<ArticleByPublishedDate>(x => x.Title.IsNotIn<ArticleByPublishedDate>(s => s.Title, w => w.Title.Contains("steve"))).OrderByDescending(x => x.Title)
                    )
                    .Any(
                        x => x.With<ArticleByPublishedDate>(x => x.Title.Contains("about"))
                    )
                    ;

                // Normal YesSql query
                Assert.Equal(2, await yesSqlQuery.CountAsync());
                Assert.Equal("Blog by paul about chickens", (await yesSqlQuery.FirstOrDefaultAsync()).Title);

                // Parsed query
                Assert.Equal(2, await filterQuery.CountAsync());
                Assert.Equal("Blog by paul about chickens", (await filterQuery.FirstOrDefaultAsync()).Title);
            }
        }
        #endregion
    }
}
