YesSql
=============

A .NET document database interface for relational databases, because in SQL we (still) trust !

[![Build](https://github.com/sebastienros/yessql/actions/workflows/build.yml/badge.svg)](https://github.com/sebastienros/yessql/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/YesSql.svg)](https://www.nuget.org/packages/YesSql)
[![MyGet](https://img.shields.io/myget/yessql/vpre/yessql.svg?label=MyGet)](https://www.myget.org/feed/yessql/package/nuget/yessql)

How does it work ?
-------------------

YesSql is a .NET Core document database interface over relational databases which allows you to define documents and indexes using plain old CLR objects. The main difference
with document databases is that it uses any RDBMS to store them, which gives you all the power of SQL databases
like transactions, replication, reporting, ... But the main advantage might be that there is no magic involved, it's pure SQL !

A video about YesSql was recorded and is available here https://www.youtube.com/watch?v=D42eK6CJjF4 

FAQ
-------------------

### Aren't NoSQL databases also about map/reduce ?

YesSql has support for it too. There is a [sample project](https://github.com/sebastienros/yessql/tree/master/samples/YesSql.Samples.Hi) in the source code, and you'll see that map/reduce is fully supported by looking at the tests.

### How is the database structured ?

There is a global [Document] table. Each index is a custom class which has its own table. A reduce index also adds a bridge table in order to map many documents. 
Internally YesSql communicates with the database server using [Dapper](https://github.com/StackExchange/dapper-dot-net).

### Dude ! Why another document database ?

I know :/ Well actually I am a big fan of document databases and I am well aware that some like MongoDb and RavenDb are already top-notch ones, but __what if you want a free, transactional .NET document database__ ?

* MongoDb is not transactional, and some applications can't cope with it. RDBMS on the contrary are all transactional. 
* RavenDb (which I am a big fan of) is not free (for most usages). 
* Some companies which have invested a lot in SQL, only trust SQL, and have in-house experts.

So YesSql might be an answer for the developers who face those restrictions. If you don't care about those then please don't spend one more minute on YesSql, it's useless for you.

### I am sold, where do I start ?

The documentation is here: https://github.com/sebastienros/yessql/wiki

You can also take a look at the [sample apps](https://github.com/sebastienros/yessql/tree/master/samples) in the source code.

Telemetry
-------------------

YesSql emits spans and metrics through `System.Diagnostics`, using an `ActivitySource` and a `Meter` both named `YesSql` (`YesSqlTelemetry.Name`). It doesn't depend on the OpenTelemetry SDK, and with no listener a command only pays for a few field reads and the increment of a counter.

### Subscribing

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(YesSqlTelemetry.Name))
    .WithMetrics(metrics => metrics
        .AddMeter(YesSqlTelemetry.Name)
        .AddMeter([.. YesSqlTelemetry.DriverMeters]));
```

An application configuring the names as strings subscribes to `YesSql`, and optionally to the drivers described below. A name that nothing emits is ignored.

### Spans

Each command run by YesSql is a span with the `Client` kind, named `{db.operation.name} {db.collection.name}`, e.g. `SELECT tp_PersonByName`. This covers queries, writes, batches, schema changes, the id generators, commits and rollbacks.

| Attribute | Description |
| --- | --- |
| `db.system.name` | `sqlite`, `microsoft.sql_server`, `postgresql`, `mysql`, or `other_sql` for a custom dialect not overriding `ISqlDialect.TelemetrySystemName` |
| `db.namespace` | The name of the database, from the connection. It is always `main` with SQLite |
| `db.operation.name` | `SELECT`, `INSERT`, `UPDATE`, `DELETE`, `COMMIT`, `ROLLBACK`, the first keyword of a schema change (`CREATE`, `ALTER`, `DROP`), or for a batch `BATCH INSERT` or `BATCH` when its statements differ |
| `db.collection.name` | The table, with its prefix and schema. For a query, the table it selects from, which it may join to others. Not set on a batch, which usually involves several tables, nor on a commit or rollback |
| `db.operation.batch.size` | The number of statements of a batch |
| `db.query.text` | The SQL text. Only with `IncludeQueryText` |
| `error.type` | The full name of the exception type, with the span status set to `Error` and an `exception` event |

The duration of a span ends with the results, except for an index streamed with `ToAsyncEnumerable()` or `ListAsync()` on `QueryIndex<T>()`, where it ends with the first row, as the rest of the time depends on the consumer.

Each flush is also a `yessql.session.flush` span with the `Internal` kind, parent of its commands, with `db.system.name`, `yessql.commands`, `yessql.documents.inserted`, `yessql.documents.updated` and `yessql.documents.deleted`. A flush having nothing to write, as when a query triggers it with only tracked documents, still shows the time spent looking for changes.

The commands an application runs itself on the connection returned by `ISession.CreateConnectionAsync()` aren't traced by YesSql.

Initializing a store detects what already exists by running commands that can fail: a `SELECT` on a missing document table, and a `CREATE` on the existing table of `DbBlockIdGenerator`. They are reported as any failed command, so expect a few of them when a store starts.

### Metrics

| Instrument | Type | Unit | Attributes | Description |
| --- | --- | --- | --- | --- |
| `db.client.operation.duration` | Histogram | `s` | `db.system.name`, `db.namespace`, `db.operation.name`, `error.type` | Duration of each command, commit and rollback |
| `yessql.session.flush.duration` | Histogram | `s` | `db.system.name`, `error.type` | Duration of each flush |
| `yessql.session.commands` | Histogram | `{command}` | `db.system.name` | Number of commands run by a session, recorded when it is disposed if it ran any. A high value reveals an N+1 query pattern |
| `yessql.session.documents` | Histogram | `{document}` | `db.system.name`, `yessql.document.operation` (`read`, `insert`, `update`, `delete`) | Number of documents read or written by a session, recorded when it is disposed |

Neither the SQL text nor the table are recorded on metrics, to keep the number of series bounded: a table name also holds the prefix of a tenant, and there can be many. Tables are on the spans.

The histograms of durations recommend the bucket boundaries of the OpenTelemetry semantic conventions on .NET 9 and later. On .NET 8, where this isn't available, configure them with a view.

### Options

```csharp
var store = await StoreFactory.CreateAndInitializeAsync(new Configuration()
    .UseSqLite("Data Source=yessql.db")
    .ConfigureTelemetry(telemetry =>
    {
        // Add the SQL text to the spans, in db.query.text. False by default: the statements generated by
        // YesSql are parameterized, but raw fragments given to a query by an application are included as is.
        telemetry.IncludeQueryText = true;

        // Add tags to all the spans and measurements of this store, typically to tell tenants apart.
        // They are recorded on metrics too, so their values should be bounded.
        telemetry.Tags.Add(new("tenant.name", "acme"));
    }));
```

### Totals per session and per request

To spot an N+1 query pattern without any listener, a session exposes its totals, which can still be read once it is disposed:

```csharp
await using var session = store.CreateSession();

// ...

logger.LogInformation("{Commands} commands, {Documents} documents", session.Statistics.Commands, session.Statistics.DocumentsRead);
```

To total the work of an asynchronous flow, whatever the number of sessions involved, start a scope, for instance in a middleware:

```csharp
app.Use(async (context, next) =>
{
    using var scope = YesSqlTelemetry.BeginScope();

    await next(context);

    Activity.Current?.SetTag("app.db.commands", scope.Statistics.Commands);
    Activity.Current?.SetTag("app.db.duration", scope.Statistics.CommandDuration.TotalSeconds);
});
```

`YesSqlStatistics` holds `Commands`, `CommandDuration`, `Flushes`, `DocumentsRead`, `DocumentsInserted`, `DocumentsUpdated` and `DocumentsDeleted`. Commits and rollbacks aren't commands, and a batch counts as one. `CommandDuration` needs timing the commands, so it is only measured while a scope is active or a listener subscribes to `YesSql`, and is otherwise zero for a session. A query served by query gating runs once, and counts for the session that ran it. The id generators use their own connections, so their commands count for a scope but not for a session.

### Database drivers

YesSql already covers the commands of every provider. What only a driver knows is the state of its connections, so subscribing to the meters in `YesSqlTelemetry.DriverMeters` is the recommended way to get the connection pools, without having to know which provider is in use. What the drivers YesSql references emit:

| Provider | Driver | `ActivitySource` | `Meter` | Otherwise |
| --- | --- | --- | --- | --- |
| PostgreSQL | Npgsql 10 | `Npgsql` | `Npgsql` | |
| MySQL | MySqlConnector 2.6 | `MySqlConnector` | `MySqlConnector` | |
| SQL Server | Microsoft.Data.SqlClient 7 | None | None | A `SqlClientDiagnosticListener` diagnostic listener, and event counters |
| SQLite | Microsoft.Data.Sqlite 10 | None | None | Nothing |

Subscribing to the drivers' sources is optional, since it adds a span in each YesSql span, with the details of the connection, and for Npgsql the SQL text whatever `IncludeQueryText` says. `YesSqlTelemetry.DriverActivitySources` lists them. To keep one client span per database call, the YesSql span then changes its kind to `Internal`, which it detects from the listeners of the source named by `ISqlDialect.DriverTelemetryName`.

Npgsql also publishes its own `db.client.operation.duration`, so with both meters each command is measured twice, once per instrumentation scope, and a sum across scopes counts it twice. It can't be turned off in the driver, so drop it when subscribing:

```csharp
metrics.AddView(instrument => YesSqlTelemetry.IsDuplicateDriverInstrument(instrument)
    ? MetricStreamConfiguration.Drop
    : null);
```

This keeps the instrument of YesSql, which has the operation and the custom tags, and the other instruments of the driver. It also drops the durations of the Npgsql commands that don't go through YesSql, such as those of EF Core, for the whole process. An application needing them should keep both instruments and filter on the scope instead.

Performance
-------------------

### PostgreSQL: prepared statements

YesSql generates the same SQL text for the same queries and commands, so PostgreSQL can reuse prepared statements instead of parsing and planning them on each execution. Npgsql prepares the most frequent statements automatically, and keeps them across pooled connections, when it's enabled in the connection string:

```
Server=...;Database=...;Max Auto Prepare=50;Auto Prepare Min Usages=2
```

With the benchmarks in `samples/YesSql.Samples.Performance` on a local server, it makes inserts and updates about twice as fast, and most queries 25% to 65% faster.

Prepared statements use generic plans once PostgreSQL considers them as good as custom ones, which depends on up-to-date table statistics. Tables that change a lot quickly, like freshly loaded ones, may need an `ANALYZE` before their statistics, and the plans based on them, are accurate.

### Creating indexes without blocking writes

Creating an index on a large table can block the writes to that table until it is done. By default, the indexes that `ISchemaBuilder.AlterTable` creates are built while the table can still be written to, when the database supports it:

| Provider | SQL | Conditions |
| --- | --- | --- |
| PostgreSQL | `create index concurrently` | Only with a schema builder that doesn't use a transaction |
| SQL Server | `with (online = on)` | Enterprise and Developer editions, Azure SQL Database and Managed Instance. The other editions create the index the regular way |
| MySQL | `algorithm=inplace lock=none` | Fails instead of locking the table if the index can't be created that way |
| SQLite | Not supported | |

PostgreSQL can't create an index concurrently in a transaction, so it is only done with a schema builder created from a connection instead of a transaction. Each of its statements then runs on its own, and a failure doesn't revert the previous ones. A concurrent index creation that fails also leaves an invalid index behind. Since an index that already exists isn't created again, it has to be dropped before trying again, or it is kept invalid.

```csharp
await using var connection = store.Configuration.ConnectionFactory.CreateConnection();
await connection.OpenAsync();

var builder = new SchemaBuilder(store.Configuration, connection);

await builder.AlterIndexTableAsync<PersonByName>(table => table
    .CreateIndex("IDX_PersonByName_Name", "Name", "DocumentId"));
```

The indexes YesSql creates with the index tables, which are still empty, are always created the regular way. To create all the indexes the regular way, disable it in the configuration:

```csharp
configuration.DisableConcurrentIndexCreation();
```

Development
-------------------

- Development and pull-requests are done based on the `main` branch.
- Every build and pull-request are tested against Sqlite, PostgreSQL, SQL Server and MySql.
- The latest NuGet release is tagged and available in the [releases](https://github.com/sebastienros/yessql/releases) page.
- The `main` branch is also published on [MyGet](https://www.myget.org/feed/yessql/package/nuget/yessql) automatically.
