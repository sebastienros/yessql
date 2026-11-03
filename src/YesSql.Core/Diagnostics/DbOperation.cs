using System.Data.Common;

namespace YesSql.Diagnostics
{
    /// <summary>
    /// Describes a database operation about to be executed, with what YesSql knows about it.
    /// </summary>
    internal readonly struct DbOperation
    {
        public const string Select = "SELECT";
        public const string Insert = "INSERT";
        public const string Update = "UPDATE";
        public const string Delete = "DELETE";
        public const string Commit = "COMMIT";
        public const string Rollback = "ROLLBACK";

        /// <param name="configuration">The configuration of the store.</param>
        /// <param name="connection">The connection executing the operation.</param>
        /// <param name="name">The name of the operation, e.g. <see cref="Select"/>.</param>
        /// <param name="table">The name of the table, as given to the table name convention, without prefix nor schema.</param>
        /// <param name="sql">The SQL text.</param>
        /// <param name="statistics">The statistics of the session executing the operation, if any.</param>
        public DbOperation(IConfiguration configuration, DbConnection connection, string name, string table, string sql, YesSqlStatistics statistics = null)
        {
            Configuration = configuration;
            Connection = connection;
            Name = name;
            Table = table;
            Sql = sql;
            Statistics = statistics;
        }

        public IConfiguration Configuration { get; }
        public DbConnection Connection { get; }
        public string Name { get; }
        public string Table { get; }
        public string Sql { get; }
        public YesSqlStatistics Statistics { get; }

        /// <summary>
        /// Gets the number of statements of a batch, or 0 when the operation isn't one.
        /// </summary>
        public int BatchSize { get; init; }

        /// <summary>
        /// Gets whether the operation is a command for the statistics, unlike a commit or a rollback.
        /// </summary>
        public bool IsCommand { get; init; } = true;
    }
}
