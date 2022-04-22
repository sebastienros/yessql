using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using YesSql.Commands;
using YesSql.Indexes;
using YesSql.Provider.Oracle;
using YesSql.Sql;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;
using Xunit;
using Xunit.Abstractions;

namespace YesSql.Tests
{
    public class OracleTests : CoreTests
    {
        private const string OracleAllSupportedTypesTableName = "OracleAllSupportedTypes";
        private const string ModifyColumnTestTableName = "ModifyColumnTest";
        private const string OracleDifferentLengthTableName = "OracleDifferentLength";
        private const string DropIndexTestTableName = "DropIndexTest";

        public static string ConnectionString => Environment.GetEnvironmentVariable("ORACLE_CONNECTION_STRING") ?? @"Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = localhost)(PORT = 1521)) (CONNECT_DATA = (SERVER = DEDICATED) (SERVICE_NAME = orcl)));User Id=yessql_test;Password=password;";
        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseOracle(ConnectionString)
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                ;
        }

        protected override string DecimalColumnDefinitionFormatString => "number({0},{1})";

        public OracleTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override void OnCleanDatabase(SchemaBuilder builder, DbTransaction transaction)
        {
            base.OnCleanDatabase(builder, transaction);

            try
            {
                builder.DropTable("Content");
            }
            catch
            {
                // ignored
            }

            try
            {
                builder.DropTable("Collection1_Content");
            }
            catch
            {
                // ignored
            }

            try
            {
                builder.DropTable(OracleAllSupportedTypesTableName);
            }
            catch
            {
                // ignored
            }

            try
            {
                builder.DropTable(ModifyColumnTestTableName);
            }
            catch
            {
                // ignored
            }

            try
            {
                builder.DropTable(OracleDifferentLengthTableName);
            }
            catch
            {
                // ignored
            }
            try
            {
                builder.DropTable(DropIndexTestTableName);
            }
            catch
            {
                // ignored
            }

        }

        [Fact]
        public void ShouldCreateTablesWithDifferentStringLenth()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.CreateTable(OracleDifferentLengthTableName, column => column
                    .Column<string>("default")
                    .Column<string>("255", c => c.WithLength(255))
                    .Column<string>("2000", c => c.WithLength(2000))
                    .Column<string>("2001", c => c.WithLength(2001))
                    .Column<string>("4000", c => c.WithLength(4000))
                    .Column<string>("16777216", c => c.WithLength(16777216)));

                    transaction.Commit();
                }
            }
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var sqlForDataTypes = $"SELECT column_name as \"ColumnName\", data_type as \"DataType\" " +
                        $" FROM all_tab_columns where table_name = \'{TablePrefix}{OracleDifferentLengthTableName}\'";
                    var dataTypes = connection.Query<TableColumnInfo>(sqlForDataTypes).ToList();

                    Assert.Equal(Dapper.Oracle.OracleMappingType.NVarchar2, dataTypes.FirstOrDefault(dt => dt.ColumnName == "default")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.NVarchar2, dataTypes.FirstOrDefault(dt => dt.ColumnName == "255")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.NVarchar2, dataTypes.FirstOrDefault(dt => dt.ColumnName == "2000")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.NClob, dataTypes.FirstOrDefault(dt => dt.ColumnName == "2001")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.NClob, dataTypes.FirstOrDefault(dt => dt.ColumnName == "4000")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.NClob, dataTypes.FirstOrDefault(dt => dt.ColumnName == "16777216")?.OracleMappingType);

                    transaction.Commit();
                }
            }



        }
        [Fact]
        public void ShouldCreateTablesAllSupportedTypes()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.CreateTable(OracleAllSupportedTypesTableName, column => column
                    .Column<object>("blob")
                    .Column<DateTime>("timestamp")
                    .Column<bool>("number(1)")
                    .Column<decimal>("number")
                    .Column<float>("binary_float")
                    .Column<double>("binary_double")
                    .Column<short>("number(5,0)")
                    .Column<int>("number(9,0)")
                    .Column<long>("number(19,0)")
                    .Column<ushort>("unumber(5,0)")
                    .Column<uint>("unumber(9,0)")
                    .Column<ulong>("unumber(19,0)")
                    .Column<string>("nvarchar2(255)")
                    .Column<sbyte>("integer"));
                    transaction.Commit();
                }
            }
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var sqlForDataTypes = $"SELECT column_name as \"ColumnName\", data_type as \"DataType\" " +
                        $" FROM all_tab_columns where table_name = \'{TablePrefix}{OracleAllSupportedTypesTableName}\'";
                    var dataTypes = connection.Query<TableColumnInfo>(sqlForDataTypes);

                    Assert.Equal(Dapper.Oracle.OracleMappingType.Blob, dataTypes.FirstOrDefault(dt => dt.ColumnName == "blob")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.TimeStamp, dataTypes.FirstOrDefault(dt => dt.ColumnName == "timestamp")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.Int32, dataTypes.FirstOrDefault(dt => dt.ColumnName == "number(1)")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.Int32, dataTypes.FirstOrDefault(dt => dt.ColumnName == "number")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.Int32, dataTypes.FirstOrDefault(dt => dt.ColumnName == "number(5,0)")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.Int32, dataTypes.FirstOrDefault(dt => dt.ColumnName == "number(9,0)")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.Int32, dataTypes.FirstOrDefault(dt => dt.ColumnName == "number(19,0)")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.Int32, dataTypes.FirstOrDefault(dt => dt.ColumnName == "unumber(5,0)")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.Int32, dataTypes.FirstOrDefault(dt => dt.ColumnName == "unumber(9,0)")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.Int32, dataTypes.FirstOrDefault(dt => dt.ColumnName == "unumber(19,0)")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.Int32, dataTypes.FirstOrDefault(dt => dt.ColumnName == "integer")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.BinaryFloat, dataTypes.FirstOrDefault(dt => dt.ColumnName == "binary_float")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.BinaryDouble, dataTypes.FirstOrDefault(dt => dt.ColumnName == "binary_double")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.NVarchar2, dataTypes.FirstOrDefault(dt => dt.ColumnName == "nvarchar2(255)")?.OracleMappingType);
                }
            }
        }

        [Fact]
        public async Task ShouldDropIndex()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.CreateTable(DropIndexTestTableName, column => column
                    .Column<string>("Test")
                    .Column<sbyte>("integer"));

                    transaction.Commit();
                }
            }

            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.AlterTable(DropIndexTestTableName, table => table
                       .CreateIndex("IDX_Index", "Test"));
                    transaction.Commit();
                    var sqlForDataTypes = $"SELECT  count(*) FROM all_indexes WHERE table_name = '{TablePrefix}{DropIndexTestTableName}\'";
                    Assert.Equal(1, await connection.QuerySingleAsync<int>(sqlForDataTypes));
                }
            }
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.AlterTable(DropIndexTestTableName, table => table
                       .DropIndex("IDX_Index"));
                    transaction.Commit();

                    var sqlForDataTypes = $"SELECT  count(*) FROM all_indexes WHERE table_name = '{TablePrefix}{DropIndexTestTableName}\'";
                    Assert.Equal(0, await connection.QuerySingleAsync<int>(sqlForDataTypes));
                }
            }
        }

        [Fact]
        public async Task ShouldModifyColumn()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.CreateTable(ModifyColumnTestTableName, column => column
                    .Column<string>("Test"));

                    transaction.Commit();
                }
            }

            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.AlterTable(ModifyColumnTestTableName, table => table
                       .AlterColumn("Test", column => column.WithType(typeof(int), 1)));
                    transaction.Commit();
                    var sqlForDataTypes = $"SELECT column_name as \"ColumnName\", data_type as \"DataType\" " +
                        $" FROM all_tab_columns where table_name = \'{TablePrefix}{ModifyColumnTestTableName}\'";
                    var dataTypes = await connection.QueryAsync<TableColumnInfo>(sqlForDataTypes);

                    Assert.Equal("NUMBER", dataTypes.First().DataType);
                }
            }
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    var ex = Assert.Throws<Exception>(() =>
                    {
                        builder.AlterTable(ModifyColumnTestTableName, table => table
                           .AlterColumn("Test", column => column.WithType(typeof(object), 1, 0)));
                        transaction.Commit();
                    });
                    Assert.Equal("Error while executing data migration: you need to specify the field's type in order to change its properties", ex.Message);
                }
            }
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.AlterTable(ModifyColumnTestTableName, table => table
                       .AlterColumn("Test", column => column.WithType(typeof(string), 2).WithDefault("ab")));

                    transaction.Commit();
                    var sqlForDataTypes = $"SELECT column_name as \"ColumnName\", data_type as \"DataType\", data_default as \"DataDefault\" " +
                        $" FROM all_tab_columns where table_name = \'{TablePrefix}{ModifyColumnTestTableName}\'";
                    var dataTypes = await connection.QuerySingleAsync(sqlForDataTypes);

                    Assert.Equal("NVARCHAR2", dataTypes.DataType);
                    Assert.Equal("'ab' ", dataTypes.DataDefault);
                }
            }
        }

        [Fact]
        public override async Task AllDataTypesShouldBeStored()
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
                var index = new TypesIndex
                {
                    ValueDateTime = valueDateTime,
                    ValueGuid = valueGuid,
                    ValueBool = valueBool,
                    ValueDateTimeOffset = valueDateTimeOffset,
                    ValueTimeSpan = valueTimeSpan
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

                Assert.Equal(valueDateTime, index.ValueDateTime);
                Assert.Equal(valueGuid, index.ValueGuid);
                Assert.Equal(valueBool, index.ValueBool);
                // stored as UTC datetime
                Assert.Equal(valueDateTimeOffset.UtcDateTime, index.ValueDateTimeOffset.LocalDateTime);
                //TODO Oracle Dapper not supported TimeSpan from Ticks converting
                //Assert.Equal(valueTimeSpan, index.ValueTimeSpan); 

                Assert.Equal(0, index.ValueDecimal);
                Assert.Equal(0, index.ValueDouble);
                Assert.Equal(0, index.ValueFloat);
                Assert.Equal(0, index.ValueInt);
                Assert.Equal(0, index.ValueLong);
                Assert.Equal(0, index.ValueShort);
            }
        }

        [Fact]
        public override async Task AllDataTypesShouldBeQueryableWithProperties()
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
                var index = new TypesIndex
                {
                    ValueDateTime = valueDateTime,
                    ValueGuid = valueGuid,
                    ValueBool = valueBool,
                    ValueDateTimeOffset = valueDateTimeOffset,
                    ValueTimeSpan = valueTimeSpan
                };

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
                // stored as UTC datetime
                Assert.Equal(valueDateTimeOffset.UtcDateTime, index.ValueDateTimeOffset.LocalDateTime);
                //TODO Oracle Dapper not supported TimeSpan from Ticks converting
                //Assert.Equal(valueTimeSpan, index.ValueTimeSpan); 

                Assert.Equal(0, index.ValueDecimal);
                Assert.Equal(0, index.ValueDouble);
                Assert.Equal(0, index.ValueFloat);
                Assert.Equal(0, index.ValueInt);
                Assert.Equal(0, index.ValueLong);
                Assert.Equal(0, index.ValueShort);
            }
        }

        [Fact]
        public override async Task AllDataTypesShouldBeQueryableWithConstants()
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
                var index = new TypesIndex
                {
                    ValueDateTime = valueDateTime,
                    ValueGuid = valueGuid,
                    ValueBool = valueBool,
                    ValueDateTimeOffset = valueDateTimeOffset,
                    ValueTimeSpan = valueTimeSpan
                };

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
                && x.ValueDateTimeOffset == new DateTimeOffset(valueDateTime, new TimeSpan(1, 2, 0))
                && x.ValueTimeSpan == new TimeSpan(1, 2, 3, 4, 5)
                && x.ValueGuid == Guid.Parse("cf0ef7ac-b6fe-4e24-aeda-a2b45bb5654e")).FirstOrDefaultAsync();

                Assert.Equal(valueDateTime, index.ValueDateTime);
                Assert.Equal(valueGuid, index.ValueGuid);
                Assert.Equal(valueBool, index.ValueBool);
                // stored as UTC datetime
                Assert.Equal(valueDateTimeOffset.UtcDateTime, index.ValueDateTimeOffset.LocalDateTime);
                //TODO Oracle Dapper not supported TimeSpan from Ticks converting
                //Assert.Equal(valueTimeSpan, index.ValueTimeSpan); 

                Assert.Equal(0, index.ValueDecimal);
                Assert.Equal(0, index.ValueDouble);
                Assert.Equal(0, index.ValueFloat);
                Assert.Equal(0, index.ValueInt);
                Assert.Equal(0, index.ValueLong);
                Assert.Equal(0, index.ValueShort);
            }
        }

        [Fact(Skip = "Oracle ordered CaseInsensitively only if NLS_COMP = 'LINGUISTIC' and 'NLS_SORT=BINARY_CI' have been set")]
        public override async Task ShouldOrderCaseInsensitively()
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

        [Fact(Skip = "Oracle only supports read commited or serializable isolation levels. http://download.oracle.com/docs/cd/B19306_01/server.102/b14220/consist.htm#sthref1972")]
        public override Task ShouldReadUncommittedRecords()
        {
            return base.ShouldReadUncommittedRecords();
        }
    }
}
