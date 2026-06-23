using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using YesSql.Provider.Sqlite;
using YesSql.Services;
using YesSql.Sql;

namespace YesSql.Samples.Performance
{
    [MemoryDiagnoser, ShortRunJob]
    public class SqliteBenchmarks
    {
        private readonly Consumer _consumer = new();
        private IStore _store;
        private string _dbPath;
        private string _connectionString;

        [GlobalSetup]
        public void Setup()
        {
            InitializeAsync().GetAwaiter().GetResult();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            try
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (_dbPath != null && File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch { }
        }

        private async Task InitializeAsync()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"yessql-bench-{Guid.NewGuid():N}.db");
            _connectionString = $"Data Source={_dbPath};Cache=Shared";

            var configuration = new Configuration()
                .UseSqLite(_connectionString)
                .SetTablePrefix("Perf");

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

            _store.RegisterIndexes<UserIndexProvider>();

            await CreateUsersAsync();
        }

        private async Task CreateUsersAsync()
        {
            var batch = 0;
            const int batchSize = 128;
            var session = _store.CreateSession();
            foreach (var name in Names)
            {
                batch++;
                await session.SaveAsync(new User
                {
                    Email = name + "@" + name + ".name",
                    Name = name
                });

                if (batch % batchSize == 0)
                {
                    await session.SaveChangesAsync();
                    await session.DisposeAsync();
                    session = _store.CreateSession();
                }
            }

            await session.SaveChangesAsync();
            await session.DisposeAsync();
        }

        [Benchmark]
        public ISession CreateSession()
        {
            using var session = _store.CreateSession();
            return session;
        }

        [Benchmark]
        public async Task WriteDocuments()
        {
            await using var session = _store.CreateSession();
            for (var i = 0; i < 128; i++)
            {
                var name = Names[i];
                await session.SaveAsync(new User
                {
                    Email = name + "@" + name + ".name",
                    Name = name
                });
            }

            await session.SaveChangesAsync();
        }

        [Benchmark]
        public async Task QueryIndexByName()
        {
            var rnd = new Random(1);
            var names = Enumerable.Range(1, 10).Select(x => Names[rnd.Next(Names.Length - 1)]).ToArray();

            await using var session = _store.CreateSession();
            foreach (var user in await session.QueryIndex<UserByName>(x => x.Name.IsIn(names)).ListAsync())
            {
                _consumer.Consume(user);
            }
        }

        [Benchmark]
        public async Task QueryByName()
        {
            var rnd = new Random(1);
            var names = Enumerable.Range(1, 10).Select(x => Names[rnd.Next(Names.Length - 1)]).ToArray();

            await using var session = _store.CreateSession();
            foreach (var user in await session.Query<User, UserByName>(x => x.Name.IsIn(names)).ListAsync())
            {
                _consumer.Consume(user);
            }
        }

        [Benchmark]
        public async Task QueryLinqSingle()
        {
            var rnd = new Random(1);
            var name = Names[rnd.Next(Names.Length - 1)];

            await using var session = _store.CreateSession();
            foreach (var user in await session.QueryIndex<UserByName>(x => x.Name == name).ListAsync())
            {
                _consumer.Consume(user);
            }
        }

        [Benchmark]
        public async Task QueryFirstOrDefault()
        {
            var rnd = new Random(1);
            var name = Names[rnd.Next(Names.Length - 1)];

            await using var session = _store.CreateSession();
            var user = await session.Query<User, UserByName>(x => x.Name == name).FirstOrDefaultAsync();
            _consumer.Consume(user);
        }

        [Benchmark]
        public async Task CountAll()
        {
            await using var session = _store.CreateSession();
            var count = await session.QueryIndex<UserByName>().CountAsync();
            _consumer.Consume(count);
        }

        private static readonly string[] Names = Benchmarks.Names;
    }
}
