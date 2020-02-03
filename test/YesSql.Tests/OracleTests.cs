using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using YesSql.Provider.Oracle;
using YesSql.Sql;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;
using Xunit;

namespace YesSql.Tests
{
    public class OracleTests : CoreTests
    {
        public static string ConnectionString => Environment.GetEnvironmentVariable("ORACLE_CONNECTION_STRING") ?? @"Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = localhost)(PORT = 1521)) (CONNECT_DATA = (SERVER = DEDICATED) (SERVICE_NAME = orcl.mazuryv)));User Id=oracle;Password=Password12!;";
        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseOracle(ConnectionString)
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                ;
        }
        public OracleTests()
        {
        }

        protected override void OnCleanDatabase(SchemaBuilder builder, DbTransaction transaction)
        {
            base.OnCleanDatabase(builder, transaction);

            try
            {
                builder.DropTable("Content");
            }
            catch { }

            try
            {
                builder.DropTable("Collection1_Content");
            }
            catch { }
        }

        [Fact]
        public void ShoudCreateTablesWithDifferentStringLenth()
        {
            var tableName = "OracleDifferentLenth";
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.CreateTable(tableName, column => column
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
                        $" FROM all_tab_columns where table_name = \'{TablePrefix}{tableName}\'";
                    var dataTypes = connection.Query<TableColumnInfo>(sqlForDataTypes);

                    Assert.Equal(Dapper.Oracle.OracleMappingType.NVarchar2, dataTypes.FirstOrDefault(dt=>dt.ColumnName=="default")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.NVarchar2, dataTypes.FirstOrDefault(dt => dt.ColumnName == "255")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.NVarchar2, dataTypes.FirstOrDefault(dt => dt.ColumnName == "2000")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.NClob, dataTypes.FirstOrDefault(dt => dt.ColumnName == "2001")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.NClob, dataTypes.FirstOrDefault(dt => dt.ColumnName == "4000")?.OracleMappingType);
                    Assert.Equal(Dapper.Oracle.OracleMappingType.NClob, dataTypes.FirstOrDefault(dt => dt.ColumnName == "16777216")?.OracleMappingType);

                    var builder = new SchemaBuilder(_store.Configuration, transaction);
                    builder.DropTable(tableName);
                    transaction.Commit();
                }
            }



        }
        [Fact]
        public void ShoudCreateTablesAllSupportedTypes()
        {
            var tableName = "OracleAllSupportedTypes";
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.CreateTable(tableName, column => column
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
                        $" FROM all_tab_columns where table_name = \'{TablePrefix}{tableName}\'";
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

                    var builder = new SchemaBuilder(_store.Configuration, transaction);
                    builder.DropTable(tableName);
                    transaction.Commit();
                }
            }
        }

        [Fact]
        public async Task ShouldDropIndex()
        {
            var tableName = "DropIndexTest";
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.CreateTable(tableName, column => column
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

                    builder.AlterTable(tableName, table => table
                       .CreateIndex("IDX_Index", "Test"));
                    transaction.Commit();
                    var sqlForDataTypes = $"SELECT  count(*) FROM all_indexes WHERE table_name = '{TablePrefix}{tableName}\'";
                    Assert.Equal(1, await connection.QuerySingleAsync<int>(sqlForDataTypes));
                }
            }
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.AlterTable(tableName, table => table
                       .DropIndex("IDX_Index"));
                    transaction.Commit();

                    var sqlForDataTypes = $"SELECT  count(*) FROM all_indexes WHERE table_name = '{TablePrefix}{tableName}\'";
                    Assert.Equal(0, await connection.QuerySingleAsync<int>(sqlForDataTypes));
                }
            }
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);
                    builder.DropTable(tableName);
                    transaction.Commit();
                }
            }
        }
        [Fact]
        public async Task ShouldModifyColumn()
        {
            var tableName = "ModifyColumnTest";
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.CreateTable(tableName, column => column
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

                    builder.AlterTable(tableName, table => table
                       .AlterColumn("Test", column=>column.WithType(System.Data.DbType.Int32,1)));
                    transaction.Commit();
                    var sqlForDataTypes = $"SELECT column_name as \"ColumnName\", data_type as \"DataType\" " +
                        $" FROM all_tab_columns where table_name = \'{TablePrefix}{tableName}\'";
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
                        builder.AlterTable(tableName, table => table
                           .AlterColumn("Test", column => column.WithType(System.Data.DbType.Object, 1, 0)));
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

                    builder.AlterTable(tableName, table => table
                       .AlterColumn("Test", column => column.WithType(System.Data.DbType.String, 2).WithDefault("ab")));

                    transaction.Commit();
                    var sqlForDataTypes = $"SELECT column_name as \"ColumnName\", data_type as \"DataType\", data_default as \"DataDefault\" " +
                        $" FROM all_tab_columns where table_name = \'{TablePrefix}{tableName}\'";
                    var dataTypes = await connection.QuerySingleAsync(sqlForDataTypes);

                    Assert.Equal("NVARCHAR2", dataTypes.DataType);
                    Assert.Equal("'ab' ", dataTypes.DataDefault);
                }
            }

            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);
                    builder.DropTable(tableName);
                    transaction.Commit();
                }
            }
        }

        [Fact(Skip= "Oracle ordered CaseInsensitively only if NLS_COMP = 'LINGUISTIC' and 'NLS_SORT=BINARY_CI' have been set")]

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
