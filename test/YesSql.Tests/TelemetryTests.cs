using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YesSql.Commands;
using YesSql.Diagnostics;
using YesSql.Indexes;
using YesSql.Provider.MySql;
using YesSql.Provider.PostgreSql;
using YesSql.Provider.Sqlite;
using YesSql.Provider.SqlServer;
using YesSql.Sql;
using YesSql.Tests.Commands;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    /// <summary>
    /// The behaviors of the telemetry which don't depend on the provider, running on Sqlite. The ones covered for each
    /// provider are in <see cref="CoreTests"/>. They run on their own as they check the path without any listener, which
    /// is global to the process.
    /// </summary>
    [Collection("Telemetry")]
    public sealed class TelemetryTests : IAsyncLifetime
    {
        private const string TablePrefix = "tel_";

        private readonly TemporaryFolder _tempFolder = new();
        private IStore _store;

        private IConfiguration Configuration => _store.Configuration;

        public async ValueTask InitializeAsync()
        {
            var configuration = new Configuration()
                .UseSqLite("Data Source=" + _tempFolder.Folder + "yessql.db;Cache=Shared")
                .SetTablePrefix(TablePrefix)
                .UseDefaultIdGenerator();

            _store = await StoreFactory.CreateAndInitializeAsync(configuration);

            await using var connection = configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync(configuration.IsolationLevel);

            await new SchemaBuilder(configuration, transaction).CreateMapIndexTableAsync<PersonByName>(table => table
                .Column<string>(nameof(PersonByName.SomeName)));

            await transaction.CommitAsync();

            _store.RegisterIndexes<PersonIndexProvider>();
        }

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            _tempFolder.Dispose();

            return ValueTask.CompletedTask;
        }

        [Fact]
        public void ShouldUseTheDocumentedNames()
        {
            Assert.Equal("YesSql", YesSqlTelemetry.Name);
            Assert.Equal("YesSql", YesSqlTelemetry.ActivitySource.Name);
            Assert.Equal("YesSql", YesSqlTelemetry.Meter.Name);
        }

        [Fact]
        public void ShouldDescribeProviders()
        {
            ISqlDialect[] dialects = [new SqliteDialect(), new SqlServerDialect(), new PostgreSqlDialect(), new MySqlDialect()];

            Assert.Equal(["sqlite", "microsoft.sql_server", "postgresql", "mysql"], dialects.Select(x => x.TelemetrySystemName));
            Assert.Equal([null, null, "Npgsql", "MySqlConnector"], dialects.Select(x => x.DriverTelemetryName));

            // The lists an application subscribes to before it knows the providers of its stores.
            var drivers = dialects.Select(x => x.DriverTelemetryName).Where(x => x != null).ToArray();

            Assert.Equal(drivers, YesSqlTelemetry.DriverActivitySources);
            Assert.Equal(drivers, YesSqlTelemetry.DriverMeters);
        }

        [Fact]
        public void ShouldOnlyFindTheDurationOfNpgsqlDuplicate()
        {
            using var npgsql = new Meter("Npgsql");
            using var mySql = new Meter("MySqlConnector");
            using var yesSql = new Meter(YesSqlTelemetry.Name);

            Assert.True(YesSqlTelemetry.IsDuplicateDriverInstrument(npgsql.CreateHistogram<double>("db.client.operation.duration")));
            Assert.False(YesSqlTelemetry.IsDuplicateDriverInstrument(npgsql.CreateCounter<int>("db.client.operation.failed")));
            Assert.False(YesSqlTelemetry.IsDuplicateDriverInstrument(mySql.CreateHistogram<double>("db.client.operation.duration")));
            Assert.False(YesSqlTelemetry.IsDuplicateDriverInstrument(yesSql.CreateHistogram<double>("db.client.operation.duration")));
        }

        [Fact]
        public async Task ShouldCostNothingWithoutListener()
        {
            Assert.False(YesSqlInstrumentation.IsEnabled);

            await using var session = (Session)_store.CreateSession();

            await session.SaveAsync(new Person { Firstname = "Bill" });
            await session.SaveChangesAsync();

            Assert.Null((await session.Query<Person>().ListAsync()).Single().Lastname);
            Assert.Null(Activity.Current);

            // Counted, as a session always knows its own work, but not timed.
            Assert.True(session.Statistics.Commands > 0);
            Assert.Equal(TimeSpan.Zero, session.Statistics.CommandDuration);

            // Executing an operation goes straight to the provider, without any allocation.
            var statistics = new YesSqlStatistics();
            var result = Task.FromResult(1);
            Func<Task<int>, Task<int>> execute = static state => state;
            var operation = new DbOperation(Configuration, null, DbOperation.Select, "Document", "select", statistics);

            _ = YesSqlInstrumentation.ExecuteAsync(operation, result, execute);

            var before = GC.GetAllocatedBytesForCurrentThread();

            for (var i = 0; i < 1000; i++)
            {
                Assert.Same(result, YesSqlInstrumentation.ExecuteAsync(operation, result, execute));
            }

            Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
            Assert.Equal(1001, statistics.Commands);
        }

        [Fact]
        public async Task ShouldEmitASpanForEachCommand()
        {
            using var telemetry = new TelemetryCollector();

            await using (var session = _store.CreateSession())
            {
                await session.SaveAsync(new Person { Firstname = "Bill" });
                await session.SaveChangesAsync();
            }

            var insert = telemetry.DbSpans.Single(x => (string)x.GetTagItem("db.collection.name") == TablePrefix + "Document" && x.DisplayName.StartsWith("INSERT", StringComparison.Ordinal));

            Assert.Equal("INSERT " + TablePrefix + "Document", insert.DisplayName);
            Assert.Equal(ActivityKind.Client, insert.Kind);
            Assert.Equal("sqlite", insert.GetTagItem("db.system.name"));
            Assert.Equal("main", insert.GetTagItem("db.namespace"));
            Assert.Equal("INSERT", insert.GetTagItem("db.operation.name"));
            Assert.Null(insert.GetTagItem("db.query.text"));

            Assert.Contains(telemetry.DbSpans, x => x.DisplayName == "INSERT " + TablePrefix + nameof(PersonByName));
        }

        [Fact]
        public async Task ShouldIncludeQueryTextOnlyWhenOptedIn()
        {
            Configuration.ConfigureTelemetry(options => options.IncludeQueryText = true);

            using var telemetry = new TelemetryCollector();

            await using var session = _store.CreateSession();

            await session.Query<Person>().ListAsync();

            var select = telemetry.DbSpans.Single();

            Assert.Contains("select", (string)select.GetTagItem("db.query.text"), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Document", (string)select.GetTagItem("db.query.text"), StringComparison.Ordinal);
        }

        [Fact]
        public async Task ShouldUseTheTableAQuerySelectsFrom()
        {
            await using (var session = _store.CreateSession())
            {
                await session.SaveAsync(new Person { Firstname = "Bill" });
                await session.SaveChangesAsync();
            }

            using var telemetry = new TelemetryCollector();

            await using (var session = _store.CreateSession())
            {
                await session.Query<Person, PersonByName>().ListAsync();
                await session.Query<Person, PersonByName>(x => x.SomeName == "Bill").FirstOrDefaultAsync();
                await session.Query<Person>().CountAsync();
                await session.QueryIndex<PersonByName>().ListAsync();
            }

            var collections = telemetry.DbSpans.Select(x => (string)x.GetTagItem("db.collection.name")).ToArray();

            Assert.Equal(4, collections.Length);
            Assert.All(collections, Assert.NotNull);
            Assert.All(collections.Where(x => x != TablePrefix + "Document"), x => Assert.Equal(TablePrefix + nameof(PersonByName), x));
            Assert.Contains(TablePrefix + "Document", collections);
        }

        [Fact]
        public async Task ShouldEndStreamedSpansWithTheFirstRow()
        {
            await using (var session = _store.CreateSession())
            {
                await session.SaveAsync(new Person { Firstname = "Bill" });
                await session.SaveAsync(new Person { Firstname = "Steve" });
                await session.SaveChangesAsync();
            }

            using var telemetry = new TelemetryCollector();

            await using (var session = _store.CreateSession())
            {
                var count = 0;

                await foreach (var _ in session.QueryIndex<PersonByName>().ToAsyncEnumerable())
                {
                    // What the consumer does isn't part of the operation.
                    Assert.Single(telemetry.DbSpans);
                    count++;
                }

                Assert.Equal(2, count);
                Assert.Single(telemetry.DbSpans);
                Assert.Equal(1, session.Statistics.Commands);
            }
        }

        [Fact]
        public async Task ShouldRecordMetricsWithBoundedAttributes()
        {
            using var telemetry = new TelemetryCollector();

            await using (var session = _store.CreateSession())
            {
                await session.SaveAsync(new Person { Firstname = "Bill" });
                await session.SaveChangesAsync();
            }

            var durations = telemetry.Measurements.Where(x => x.Instrument == "db.client.operation.duration").ToArray();

            Assert.NotEmpty(durations);
            Assert.All(durations, measurement =>
            {
                Assert.True(measurement.Value >= 0);
                Assert.Equal(new[] { "db.namespace", "db.operation.name", "db.system.name" }, measurement.Tags.Keys.OrderBy(x => x, StringComparer.Ordinal));
                Assert.Equal("sqlite", measurement.Tags["db.system.name"]);
            });

            Assert.Contains(durations, x => (string)x.Tags["db.operation.name"] == "COMMIT");
        }

        [Fact]
        public async Task ShouldAddCustomTagsToSpansAndMetrics()
        {
            Configuration.ConfigureTelemetry(options => options.Tags.Add(new("tenant.name", "acme")));

            using var telemetry = new TelemetryCollector();

            await using (var session = _store.CreateSession())
            {
                await session.SaveAsync(new Person { Firstname = "Bill" });
                await session.SaveChangesAsync();
            }

            Assert.NotEmpty(telemetry.Spans);
            Assert.All(telemetry.Spans, x => Assert.Equal("acme", x.GetTagItem("tenant.name")));
            Assert.NotEmpty(telemetry.Measurements);
            Assert.All(telemetry.Measurements, x => Assert.Equal("acme", x.Tags["tenant.name"]));
        }

        [Fact]
        public async Task ShouldRecordFailedCommands()
        {
            using var telemetry = new TelemetryCollector();

            await using var session = _store.CreateSession();

            var exception = await Assert.ThrowsAsync<SqliteException>(() => session.Query<Person, MissingTableIndex>().ListAsync());

            var span = telemetry.DbSpans.Single();
            var type = typeof(SqliteException).FullName;

            Assert.Equal(ActivityStatusCode.Error, span.Status);
            Assert.Equal(type, span.GetTagItem("error.type"));
            Assert.Equal(exception.Message, span.StatusDescription);
            Assert.Equal("exception", span.Events.Single().Name);

            var duration = telemetry.Measurements.Single(x => x.Instrument == "db.client.operation.duration");

            Assert.Equal(type, duration.Tags["error.type"]);
        }

        [Fact]
        public async Task ShouldRecordFailedFlushes()
        {
            using var telemetry = new TelemetryCollector();

            await using var session = (Session)_store.CreateSession();

            await session.SaveAsync(new Person { Firstname = "Bill" });

            session._commands = new List<IIndexCommand> { new FailingCommand(new Document()) };

            await Assert.ThrowsAsync<NotImplementedException>(() => session.SaveChangesAsync());

            var flush = telemetry.Spans.Single(x => x.DisplayName == "yessql.session.flush");

            Assert.Equal(ActivityStatusCode.Error, flush.Status);
            Assert.Equal(typeof(NotImplementedException).FullName, flush.GetTagItem("error.type"));

            var duration = telemetry.Measurements.Single(x => x.Instrument == "yessql.session.flush.duration");

            Assert.Equal(typeof(NotImplementedException).FullName, duration.Tags["error.type"]);
        }

        [Fact]
        public async Task ShouldKeepTheProviderObjects()
        {
            using var telemetry = new TelemetryCollector();

            await using var session = _store.CreateSession();

            await session.SaveAsync(new Person { Firstname = "Bill" });
            await session.FlushAsync();

            // The connections aren't wrapped: what's specific to the provider keeps working.
            var connection = Assert.IsType<SqliteConnection>(await session.CreateConnectionAsync());
            var transaction = Assert.IsType<SqliteTransaction>(session.CurrentTransaction);

            Assert.Same(connection, transaction.Connection);
            Assert.NotEmpty(telemetry.DbSpans);

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT COUNT(*) FROM " + TablePrefix + "Document";

            Assert.Equal(1L, await command.ExecuteScalarAsync());

            SqliteConnection.ClearPool(connection);
        }

        [Fact]
        public async Task ShouldTraceTheTransaction()
        {
            using var telemetry = new TelemetryCollector();

            int id;

            await using (var session = _store.CreateSession())
            {
                var person = new Person { Firstname = "Bill" };

                await session.SaveAsync(person);
                await session.SaveChangesAsync();

                id = person.Id;
            }

            var commit = telemetry.DbSpans.Single(x => (string)x.GetTagItem("db.operation.name") == "COMMIT");
            var flush = telemetry.Spans.Single(x => x.DisplayName == "yessql.session.flush");

            Assert.All(
                telemetry.DbSpans.Where(x => (string)x.GetTagItem("db.operation.name") == "INSERT"),
                x => Assert.Equal(flush.SpanId, x.ParentSpanId));

            Assert.True(commit.StartTimeUtc >= flush.StartTimeUtc + flush.Duration);

            await using (var session = _store.CreateSession())
            {
                var person = await session.GetAsync<Person>(id);

                person.Firstname = "Steve";
                await session.SaveAsync(person);
                await session.FlushAsync();

                // Disposing without saving rolls the transaction back.
            }

            Assert.Single(telemetry.DbSpans, x => (string)x.GetTagItem("db.operation.name") == "ROLLBACK");

            await using (var session = _store.CreateSession())
            {
                Assert.Equal("Bill", (await session.GetAsync<Person>(id)).Firstname);
            }
        }

        [Fact]
        public async Task ShouldDescribeFlushes()
        {
            await using (var session = _store.CreateSession())
            {
                await session.SaveAsync(new Person { Firstname = "Deleted" });
                await session.SaveAsync(new Person { Firstname = "Updated" });
                await session.SaveChangesAsync();
            }

            using var telemetry = new TelemetryCollector();

            await using (var session = _store.CreateSession())
            {
                var persons = await session.Query<Person>().ListAsync();

                session.Delete(persons.Single(x => x.Firstname == "Deleted"));

                var updated = persons.Single(x => x.Firstname == "Updated");
                updated.Lastname = "Gates";
                await session.SaveAsync(updated);

                await session.SaveAsync(new Person { Firstname = "Inserted" });
                await session.SaveChangesAsync();
            }

            var flush = telemetry.Spans.Single(x => x.DisplayName == "yessql.session.flush");

            Assert.Equal(ActivityKind.Internal, flush.Kind);
            Assert.Equal("sqlite", flush.GetTagItem("db.system.name"));
            Assert.Equal(1L, flush.GetTagItem("yessql.documents.inserted"));
            Assert.Equal(1L, flush.GetTagItem("yessql.documents.updated"));
            Assert.Equal(1L, flush.GetTagItem("yessql.documents.deleted"));
            Assert.Equal((long)telemetry.DbSpans.Count(x => x.ParentSpanId == flush.SpanId), flush.GetTagItem("yessql.commands"));

            Assert.Single(telemetry.Measurements, x => x.Instrument == "yessql.session.flush.duration");
        }

        [Fact]
        public async Task ShouldExposeTheTotalsOfASession()
        {
            int[] ids;

            await using (var session = _store.CreateSession())
            {
                var persons = new[] { new Person { Firstname = "Bill" }, new Person { Firstname = "Steve" }, new Person { Firstname = "Paul" } };

                foreach (var person in persons)
                {
                    await session.SaveAsync(person);
                }

                await session.SaveChangesAsync();

                ids = persons.Select(x => x.Id).ToArray();

                Assert.Equal(3, session.Statistics.DocumentsInserted);
                Assert.Equal(1, session.Statistics.Flushes);
            }

            ISession reader = _store.CreateSession();

            await using (reader)
            {
                Assert.Equal(0, reader.Statistics.Commands);

                // An N+1 query pattern.
                foreach (var id in ids)
                {
                    await reader.GetAsync<Person>(id);
                }

                Assert.Equal(3, reader.Statistics.Commands);
                Assert.Equal(3, reader.Statistics.DocumentsRead);
            }

            // Still available once disposed.
            Assert.Equal(3, reader.Statistics.Commands);

            await using (var session = _store.CreateSession())
            {
                await session.GetAsync<Person>(ids.Select(x => (long)x).ToArray());

                Assert.Equal(1, session.Statistics.Commands);
                Assert.Equal(3, session.Statistics.DocumentsRead);

                session.Delete(await session.GetAsync<Person>(ids[0]));
                await session.SaveChangesAsync();

                Assert.Equal(1, session.Statistics.DocumentsDeleted);
            }
        }

        [Fact]
        public async Task ShouldRecordTheTotalsOfASessionAsMetrics()
        {
            int id;

            await using (var session = _store.CreateSession())
            {
                var person = new Person { Firstname = "Bill" };
                await session.SaveAsync(person);
                await session.SaveChangesAsync();
                id = person.Id;
            }

            using var telemetry = new TelemetryCollector();

            // An unused session is ignored.
            await using (_store.CreateSession())
            {
            }

            Assert.Empty(telemetry.Measurements);

            await using (var session = _store.CreateSession())
            {
                await session.GetAsync<Person>(id);
                await session.GetAsync<Person>(id);
            }

            var commands = telemetry.Measurements.Single(x => x.Instrument == "yessql.session.commands");

            Assert.Equal(2, commands.Value);
            Assert.Equal("sqlite", commands.Tags["db.system.name"]);

            var documents = telemetry.Measurements.Single(x => x.Instrument == "yessql.session.documents");

            Assert.Equal(2, documents.Value);
            Assert.Equal("read", documents.Tags["yessql.document.operation"]);
        }

        [Fact]
        public async Task ShouldTotalAFlowAcrossSessions()
        {
            YesSqlTelemetryScope outer;
            YesSqlTelemetryScope inner;

            using (outer = YesSqlTelemetry.BeginScope())
            {
                Assert.Same(outer, YesSqlTelemetryScope.Current);

                await using (var session = _store.CreateSession())
                {
                    await session.SaveAsync(new Person { Firstname = "Bill" });
                    await session.SaveChangesAsync();
                }

                var afterWrite = outer.Statistics.Commands;

                Assert.Equal(1, outer.Statistics.DocumentsInserted);
                Assert.True(afterWrite > 0);
                Assert.True(outer.Statistics.CommandDuration > TimeSpan.Zero);

                using (inner = YesSqlTelemetry.BeginScope())
                {
                    // The work of the asynchronous flows it starts counts too.
                    await Task.WhenAll(ReadAsync(), ReadAsync());

                    async Task ReadAsync()
                    {
                        await using var session = _store.CreateSession();
                        await session.Query<Person>().ListAsync();
                    }
                }

                Assert.Same(outer, YesSqlTelemetryScope.Current);
                Assert.Equal(2, inner.Statistics.Commands);
                Assert.Equal(2, inner.Statistics.DocumentsRead);
                Assert.Equal(afterWrite + 2, outer.Statistics.Commands);
            }

            Assert.Null(YesSqlTelemetryScope.Current);

            var total = outer.Statistics.Commands;

            await using (var session = _store.CreateSession())
            {
                await session.Query<Person>().ListAsync();
            }

            Assert.Equal(total, outer.Statistics.Commands);
        }

        [Fact]
        public async Task ShouldNotShareScopesWithOtherFlows()
        {
            using var scope = YesSqlTelemetry.BeginScope();

            await Task.Run(async () =>
            {
                using var other = YesSqlTelemetry.BeginScope();

                await using var session = _store.CreateSession();
                await session.Query<Person>().ListAsync();

                Assert.Equal(1, other.Statistics.Commands);
            });

            // A scope started by a child flow counts for its parents.
            Assert.Equal(1, scope.Statistics.Commands);

            await Task.Run(() => Assert.Same(scope, YesSqlTelemetryScope.Current));
        }

        [Fact]
        public void ShouldUseAnInternalSpanWhenTheDriverIsTraced()
        {
            ISqlDialect dialect = new PostgreSqlDialect();

            using (new TelemetryCollector())
            {
                Assert.Equal(ActivityKind.Client, YesSqlInstrumentation.GetCommandSpanKind(dialect));
            }

            using (new TelemetryCollector(dialect.DriverTelemetryName))
            {
                Assert.Equal(ActivityKind.Internal, YesSqlInstrumentation.GetCommandSpanKind(dialect));
                Assert.Equal(ActivityKind.Client, YesSqlInstrumentation.GetCommandSpanKind(new SqliteDialect()));
            }
        }

        [Theory]
        [InlineData("BATCH INSERT", "insert into a;", "INSERT INTO b;")]
        [InlineData("BATCH", "insert into a;", "delete from a;")]
        [InlineData("UPDATE", "update a set b = 1;")]
        public void ShouldNameBatches(string expected, params string[] statements)
        {
            Assert.Equal(expected, YesSqlInstrumentation.GetBatchOperationName(statements));
        }

        [Theory]
        [InlineData("CREATE", "CREATE TABLE [a] ()")]
        [InlineData("DROP", "  drop table a")]
        [InlineData("SELECT", "select\n1")]
        public void ShouldNameStatements(string expected, string statement)
        {
            Assert.Equal(expected, YesSqlInstrumentation.GetStatementOperationName(statement));
        }

        [Fact]
        public async Task ShouldInstrumentSchemaChangesAndIdentifiers()
        {
            using var telemetry = new TelemetryCollector();

            await using (var connection = Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();
                await using var transaction = await connection.BeginTransactionAsync(Configuration.IsolationLevel);

                await new SchemaBuilder(Configuration, transaction).CreateTableAsync("Other", table => table.Column<string>("Name"));

                await transaction.CommitAsync();
            }

            await _store.InitializeCollectionAsync("Col1");

            var create = telemetry.DbSpans.First(x => x.DisplayName == "CREATE " + TablePrefix + "Other");

            Assert.Equal("CREATE", create.GetTagItem("db.operation.name"));

            Assert.Contains(telemetry.DbSpans, x => x.DisplayName == "CREATE " + TablePrefix + "Col1_Document");
            Assert.Contains(telemetry.DbSpans, x => x.DisplayName == "SELECT " + TablePrefix + "Col1_Document");
        }

        public sealed class MissingTableIndex : MapIndex
        {
            public string Name { get; set; }
        }
    }
}
