using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using YesSql.Indexes;
using YesSql.Provider.PostgreSql;
using YesSql.Provider.Sqlite;
using YesSql.Services;
using YesSql.Sql;

namespace YesSql.Samples.Performance
{
    /// <summary>
    /// End-to-end benchmarks covering the write (insert, update, delete), load and query paths
    /// of a session, for both a non-batching provider (SQLite) and a batching one (PostgreSQL).
    /// </summary>
    /// <remarks>
    /// SQLite uses a shared in-memory database so that disk I/O doesn't hide the cost of the library itself.
    /// The PostgreSQL benchmarks expect a server on localhost:5432 (user 'root', password 'Password12!', database 'yessql'),
    /// for instance: docker run -d -e POSTGRES_USER=root -e POSTGRES_PASSWORD=Password12! -e POSTGRES_DB=yessql -p 5432:5432 postgres:16 -c synchronous_commit=off
    /// </remarks>
    [MemoryDiagnoser]
    [Config(typeof(InProcessConfig))]
    public class OptimizationBenchmarks
    {
        // The benchmarks run in-process as the out-of-process toolchain can't build the generated
        // project with the custom output paths used by this repository. A fresh database is still
        // created for each benchmark case in the global setup.
        private sealed class InProcessConfig : ManualConfig
        {
            public InProcessConfig()
            {
                AddJob(Job.Default
                    .WithLaunchCount(1)
                    .WithWarmupCount(5)
                    .WithIterationCount(30)
                    .WithToolchain(InProcessEmitToolchain.Instance));

                AddColumn(StatisticColumn.Median);
            }
        }

        private const string PostgreSqlConnectionString = "Server=localhost;Port=5432;Database=yessql;User Id=root;Password=Password12!;";
        private const int BatchSize = 128;

        private readonly Consumer _consumer = new();
        private IStore _store;
        private Microsoft.Data.Sqlite.SqliteConnection _keepAlive;
        private long[] _updateIds;
        private long[] _getIds;
        private string[] _names;
        private int _counter;

        [Params("Sqlite", "PostgreSql")]
        public string Provider { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            InitializeAsync().GetAwaiter().GetResult();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            // Closing the last connection releases the in-memory database
            _keepAlive?.Dispose();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        }

        private async Task InitializeAsync()
        {
            IConfiguration configuration;

            if (Provider == "Sqlite")
            {
                var connectionString = $"Data Source=file:yessql-opt-bench-{Guid.NewGuid():N}?mode=memory&cache=shared";

                // The in-memory database lives as long as a connection is open
                _keepAlive = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
                _keepAlive.Open();

                configuration = new Configuration().UseSqLite(connectionString);
            }
            else
            {
                configuration = new Configuration().UsePostgreSql(PostgreSqlConnectionString);
            }

            configuration.SetTablePrefix("Opt");

            // Start from an empty database on each run.
            try
            {
                await using var connection = configuration.ConnectionFactory.CreateConnection();
                await connection.OpenAsync();
                await using var transaction = await connection.BeginTransactionAsync();
                var builder = new SchemaBuilder(configuration, transaction);
                await builder.DropTableAsync("UserByName");
                await builder.DropTableAsync(configuration.TableNameConvention.GetDocumentTable(""));
                await transaction.CommitAsync();
            }
            catch { }

            _store = await StoreFactory.CreateAndInitializeAsync(configuration);

            await using (var connection = configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync();
                var builder = new SchemaBuilder(configuration, transaction);

                await builder.CreateMapIndexTableAsync<UserByName>(table => table
                    .Column<string>("Name")
                );

                await builder.AlterTableAsync("UserByName", table => table
                    .CreateIndex("IX_Name", "Name")
                );

                await transaction.CommitAsync();
            }

            _store.RegisterIndexes<UserWithIdIndexProvider>();

            _names = Benchmarks.Names.Distinct().ToArray();

            // Seed the documents. Identifiers are generated sequentially from 1 on an empty table.
            var session = _store.CreateSession();
            for (var i = 0; i < _names.Length; i++)
            {
                await session.SaveAsync(new UserWithId { Email = _names[i] + "@" + _names[i] + ".name", Name = _names[i] });

                if ((i + 1) % BatchSize == 0)
                {
                    await session.SaveChangesAsync();
                    await session.DisposeAsync();
                    session = _store.CreateSession();
                }
            }

            await session.SaveChangesAsync();
            await session.DisposeAsync();

            _updateIds = Enumerable.Range(1, BatchSize).Select(x => (long)x).ToArray();

            var rnd = new Random(1);
            _getIds = Enumerable.Range(1, 100).Select(x => (long)rnd.Next(1, _names.Length)).Distinct().ToArray();
        }

        [Benchmark]
        public ISession CreateSession()
        {
            using var session = _store.CreateSession();
            return session;
        }

        [Benchmark]
        public async Task InsertDocuments()
        {
            await using var session = _store.CreateSession();
            for (var i = 0; i < BatchSize; i++)
            {
                var name = _names[i];
                await session.SaveAsync(new UserWithId
                {
                    Email = name + "@" + name + ".name",
                    Name = name
                });
            }

            await session.SaveChangesAsync();
        }

        [Benchmark]
        public async Task UpdateDocuments()
        {
            var suffix = (++_counter).ToString();

            await using var session = _store.CreateSession();
            var users = await session.GetAsync<UserWithId>(_updateIds);

            foreach (var user in users)
            {
                user.Email = user.Name + "@" + suffix;
                await session.SaveAsync(user);
            }

            await session.SaveChangesAsync();
        }

        [Benchmark]
        public async Task InsertThenDeleteDocuments()
        {
            var ids = new long[32];

            await using (var session = _store.CreateSession())
            {
                for (var i = 0; i < ids.Length; i++)
                {
                    var user = new UserWithId { Email = "delete@me", Name = _names[i] };
                    await session.SaveAsync(user);
                    ids[i] = user.Id;
                }

                await session.SaveChangesAsync();
            }

            await using (var session = _store.CreateSession())
            {
                foreach (var user in await session.GetAsync<UserWithId>(ids))
                {
                    session.Delete(user);
                }

                await session.SaveChangesAsync();
            }
        }

        [Benchmark]
        public async Task GetByIds()
        {
            await using var session = _store.CreateSession();
            foreach (var user in await session.GetAsync<UserWithId>(_getIds))
            {
                _consumer.Consume(user);
            }
        }

        [Benchmark]
        public async Task QueryByNameIsIn()
        {
            var rnd = new Random(1);
            var names = Enumerable.Range(1, 10).Select(x => _names[rnd.Next(_names.Length - 1)]).ToArray();

            await using var session = _store.CreateSession();
            foreach (var user in await session.Query<UserWithId, UserByName>(x => x.Name.IsIn(names)).ListAsync())
            {
                _consumer.Consume(user);
            }
        }

        [Benchmark]
        public async Task QueryIndexByNameIsIn()
        {
            var rnd = new Random(1);
            var names = Enumerable.Range(1, 10).Select(x => _names[rnd.Next(_names.Length - 1)]).ToArray();

            await using var session = _store.CreateSession();
            foreach (var user in await session.QueryIndex<UserByName>(x => x.Name.IsIn(names)).ListAsync())
            {
                _consumer.Consume(user);
            }
        }

        [Benchmark]
        public async Task QueryFirstOrDefault()
        {
            var name = _names[42];

            await using var session = _store.CreateSession();
            var user = await session.Query<UserWithId, UserByName>(x => x.Name == name).FirstOrDefaultAsync();
            _consumer.Consume(user);
        }

        [Benchmark]
        public async Task QueryMultipleFilters()
        {
            var prefix = "B";
            var excluded = _names[1];

            await using var session = _store.CreateSession();
            var query = session.Query<UserWithId>()
                .With<UserByName>(x => x.Name.StartsWith(prefix))
                .Where(x => x.Name != excluded)
                .Where(x => x.Name != null)
                .OrderBy(x => x.Name)
                .Take(20);

            foreach (var user in await query.ListAsync())
            {
                _consumer.Consume(user);
            }
        }

        [Benchmark]
        public async Task QueryDocumentsPaged()
        {
            await using var session = _store.CreateSession();
            foreach (var user in await session.Query<UserWithId>().Skip(2000).Take(50).ListAsync())
            {
                _consumer.Consume(user);
            }
        }

        [Benchmark]
        public async Task QueryAllDocuments()
        {
            await using var session = _store.CreateSession();
            foreach (var user in await session.Query<UserWithId>().ListAsync())
            {
                _consumer.Consume(user);
            }
        }

        [Benchmark]
        public async Task CountIndexed()
        {
            var name = _names[42];

            await using var session = _store.CreateSession();
            _consumer.Consume(await session.Query<UserWithId, UserByName>(x => x.Name != name).CountAsync());
        }
    }

    public class UserWithId
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
    }

    public class UserWithIdIndexProvider : IndexProvider<UserWithId>
    {
        public override void Describe(DescribeContext<UserWithId> context)
        {
            context.For<UserByName>()
                .Map(user => new UserByName { Name = user.Name });
        }
    }
}
