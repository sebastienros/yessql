using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Xunit;
using YesSql;
using YesSql.Commands;
using YesSql.Commands.DocumentChanged;
using YesSql.Indexes;
using YesSql.Provider.Sqlite;
using YesSql.Services;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    /// <summary>
    /// Regression tests for the JZ fork features (branch hyzx86/5.0-ExtraDescriptors):
    /// extra index descriptors (P1-4), TypeService.ForgetType (P1-8), non-batch
    /// document command hooks (P1-9), the v6.0.0 update contract, and the default
    /// TypeService created by Store.InitializeAsync (P1-7).
    ///
    /// This class is deliberately standalone (it does not derive from CoreTests):
    /// every test instance gets its own temporary SQLite database, so the fork
    /// tests run in isolation from the shared-DB CoreTests fixture and from the
    /// provider-specific virtual overrides that only the SqliteTests/SqlServerTests
    /// subclasses provide.
    /// </summary>
    public class ForkFeatureTests : IAsyncLifetime
    {
        private const string TablePrefix = "tp";

        private TemporaryFolder _tempFolder;
        private IStore _store;

        public async ValueTask InitializeAsync()
        {
            _tempFolder = new TemporaryFolder();
            var connectionString = @"Data Source=" + _tempFolder.Folder + "yessql.db;Cache=Shared";

            var configuration = new Configuration()
                .UseSqLite(connectionString)
                .SetTablePrefix(TablePrefix)
                .UseDefaultIdGenerator()
                .WithThreadSafetyChecks()
                .SetIdentityColumnSize(IdentityColumnSize.Int64);

            _store = await StoreFactory.CreateAndInitializeAsync(configuration);
            await _store.InitializeCollectionAsync("Col1");
            _store.TypeService[typeof(Person)] = "People";

            await CoreTests.CreateTablesAsync(configuration);
        }

        public ValueTask DisposeAsync()
        {
            _store?.Dispose();
            _store = null;
            _tempFolder?.Dispose();
            _tempFolder = null;

            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task ShouldCreateDefaultTypeServiceDuringInitialize()
        {
            // P1-7: InitializeAsync must guarantee a usable TypeService when the host did not
            // inject one (plain StoreFactory usage), instead of leaving the property null.
            var configuration = new Configuration()
                .UseSqLite(@"Data Source=" + _tempFolder.Folder + "yessql-default-ts.db;Cache=Shared")
                .SetTablePrefix(TablePrefix)
                .UseDefaultIdGenerator();

            var store = StoreFactory.Create(configuration);
            Assert.Null(store.TypeService);

            await store.InitializeAsync();

            Assert.NotNull(store.TypeService);
            Assert.False(string.IsNullOrEmpty(store.TypeService[typeof(Person)]));
        }

        [Fact]
        public void ShouldForgetTypePurgePerTypeCaches()
        {
            // P1-8: ForgetType must purge the per-type cached state (name maps) so the next
            // access re-resolves and re-registers instead of serving stale entries.
            var typeService = new TypeService();

            var name = typeService[typeof(Person)];
            Assert.False(string.IsNullOrEmpty(name));

            typeService.ForgetType(typeof(Person));

            Assert.False(Contains(typeService, "typeNames", typeof(Person)));
            Assert.False(Contains(typeService, "nameTypes", name));

            // Re-access re-registers deterministically with the same computed name.
            var again = typeService[typeof(Person)];
            Assert.Equal(name, again);
            Assert.True(Contains(typeService, "typeNames", typeof(Person)));
            Assert.True(Contains(typeService, "nameTypes", name));
        }

        private static bool Contains(TypeService typeService, string fieldName, object key)
        {
            var field = typeof(TypeService).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Field '{fieldName}' not found.");

            var dictionary = field.GetValue(typeService);
            var containsKey = dictionary.GetType().GetMethod("ContainsKey", new[] { key.GetType() });
            return (bool)containsKey.Invoke(dictionary, new[] { key });
        }

        [Fact]
        public async Task ShouldMergeStaticProviderAndExtraDescriptors()
        {
            // P1-4: descriptors from the store's registered providers and from the session's
            // extra descriptors are combined with Concat — duplicates are preserved, not
            // deduplicated, so both descriptors produce index rows for the same document.
            _store.RegisterIndexes(new IIndexProvider[] { new PersonIndexProvider() });

            var person = new Person
            {
                Firstname = "mario",
                Lastname = "B"
            };

            await using var session = _store.CreateSession();
            session.BuildExtraIndexDescriptors = (type, collection) =>
            {
                if (type != typeof(Person))
                {
                    return Task.FromResult<IEnumerable<IndexDescriptor>>([]);
                }

                return Task.FromResult<IEnumerable<IndexDescriptor>>(
                [
                    new IndexDescriptor
                    {
                        Type = typeof(PersonByName),
                        IndexType = typeof(PersonByName),
                        Filter = entity => entity is Person,
                        Map = static (entity, _) =>
                        {
                            var p = (Person)entity;
                            return Task.FromResult<IEnumerable<IIndex>>(
                            [
                                new PersonByName
                                {
                                    SomeName = p.Lastname
                                }
                            ]);
                        }
                    }
                ]);
            };

            await session.SaveAsync(person);
            await session.SaveChangesAsync();

            // One row per descriptor: Firstname from the static provider, Lastname from the extra.
            var dialect = _store.Configuration.SqlDialect;
            var table = dialect.QuoteForTableName(TablePrefix + nameof(PersonByName), _store.Configuration.Schema);
            var column = dialect.QuoteForColumnName(nameof(PersonByName.SomeName));

            await using var connection = _store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            var count = await connection.QueryFirstOrDefaultAsync<long>(
                $"select count(1) from {table} where {column} in (@a, @b)",
                new { a = "mario", b = "B" });

            Assert.Equal(2, count);
        }

        [Fact]
        public async Task ShouldPersistTrackedEntityUpdateAfterResave()
        {
            // v6.0.0 update contract: hydrating a query result only identity-maps the entity;
            // a modified tracked entity must be re-registered through SaveAsync before
            // SaveChangesAsync or the change is silently dropped (5.0 detected it on its own).
            _store.RegisterIndexes(new IIndexProvider[] { new PersonIndexProvider() });

            await using (var session = _store.CreateSession())
            {
                var person = new Person
                {
                    Firstname = "before",
                    Lastname = "A"
                };

                await session.SaveAsync(person);
                await session.SaveChangesAsync();
            }

            await using (var session = _store.CreateSession())
            {
                var stored = await session.Query<Person, PersonByName>(x => x.SomeName == "before").FirstOrDefaultAsync();
                Assert.NotNull(stored);

                stored.Firstname = "after";

                // v6.0.0 contract: a plain re-SaveAsync replaces the content without touching
                // the version; the version is bumped only when the entity is saved with
                // checkConcurrency (or is a configured ConcurrentType).
                await session.SaveAsync(stored, true);
                await session.SaveChangesAsync();
            }

            await using (var session = _store.CreateSession())
            {
                var updated = await session.Query<Person, PersonByName>(x => x.SomeName == "after").FirstOrDefaultAsync();
                Assert.NotNull(updated);
                Assert.Equal(2, updated.Version);

                // The old index row was removed by the update; only the new key remains.
                var stale = await session.Query<Person, PersonByName>(x => x.SomeName == "before").FirstOrDefaultAsync();
                Assert.Null(stale);
            }
        }

        [Fact]
        public async Task ShouldFireNonBatchDocumentHooksOncePerLifecycle()
        {
            // P1-9 / upstream #717: v6.0.0 SQLite reports SupportsBatching = false, so document
            // commands execute individually and only the non-batch hook variants fire.
            _store.RegisterIndexes(new IIndexProvider[] { new PersonIndexProvider() });

            var handler = new RecordingDocumentCommandHandler();

            await using (var session = _store.CreateSession())
            {
                session.DocumentCommandHandler = handler;

                var person = new Person
                {
                    Firstname = "l1",
                    Lastname = "A"
                };

                await session.SaveAsync(person);
                await session.SaveChangesAsync();
            }

            await using (var session = _store.CreateSession())
            {
                session.DocumentCommandHandler = handler;

                var stored = await session.Query<Person, PersonByName>(x => x.SomeName == "l1").FirstOrDefaultAsync();
                Assert.NotNull(stored);

                stored.Firstname = "l2";
                await session.SaveAsync(stored);
                await session.SaveChangesAsync();
            }

            await using (var session = _store.CreateSession())
            {
                session.DocumentCommandHandler = handler;

                var stored = await session.Query<Person, PersonByName>(x => x.SomeName == "l2").FirstOrDefaultAsync();
                Assert.NotNull(stored);

                session.Delete(stored);
                await session.SaveChangesAsync();
            }

            Assert.Equal(new[] { "create", "update", "delete" }, handler.Events);
            Assert.All(handler.Documents, static document => Assert.True(document.Id > 0));
        }

        private sealed class RecordingDocumentCommandHandler : IDocumentCommandHandler
        {
            public List<string> Events { get; } = [];
            public List<Document> Documents { get; } = [];

            public Task CreatedAsync(DocumentChangeContext context)
            {
                Record("create", context.Document);
                return Task.CompletedTask;
            }

            public void CreatedInBatch(DocumentChangeInBatchContext context)
            {
                Record("create:batch", context.Document);
            }

            public Task RemovingAsync(DocumentChangeContext context)
            {
                Record("delete", context.Document);
                return Task.CompletedTask;
            }

            public void RemovingInBatch(DocumentChangeInBatchContext context)
            {
                Record("delete:batch", context.Document);
            }

            public Task UpdatedAsync(DocumentChangeContext context)
            {
                Record("update", context.Document);
                return Task.CompletedTask;
            }

            public void UpdatedInBatch(DocumentChangeInBatchContext context)
            {
                Record("update:batch", context.Document);
            }

            private void Record(string action, Document document)
            {
                Events.Add(action);
                Documents.Add(document);
            }
        }
    }
}
