using System.Data;
using System.Reflection;
using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.SqlTypes;

namespace YesSql.Core.Data
{
    public class MsSqlCeConfiguration : PersistenceConfiguration<MsSqlCeConfiguration>
    {
        protected MsSqlCeConfiguration()
        {
            Driver<CustomSqlServerCeDriver>();
        }

        public static MsSqlCeConfiguration MsSqlCe40
        {
            get { return new MsSqlCeConfiguration().Dialect<CustomMsSqlCe40Dialect>(); }

        }

        /// <summary>
        /// Custom driver so that Text/NText fields are not truncated at 4000 characters
        /// </summary>
        public class CustomSqlServerCeDriver : SqlServerCeDriver
        {
            protected override void InitializeParameter(IDbDataParameter dbParam, string name, SqlType sqlType)
            {
                base.InitializeParameter(dbParam, name, sqlType);

                PropertyInfo dbParamSqlDbTypeProperty = dbParam.GetType().GetProperty("SqlDbType");

                if (sqlType.Length <= 4000)
                {
                    return;
                }

                switch (sqlType.DbType)
                {
                    case DbType.String:
                        dbParamSqlDbTypeProperty.SetValue(dbParam, SqlDbType.NText, null);
                        break;
                    case DbType.AnsiString:
                        dbParamSqlDbTypeProperty.SetValue(dbParam, SqlDbType.Text, null);
                        break;
                }
            }
        }

        public class CustomMsSqlCe40Dialect : MsSqlCe40Dialect
        {
            public override bool SupportsVariableLimit
            {
                get { return true; }
            }
        }
    }
}