using FluentNHibernate.Cfg.Db;
using NHibernate.Dialect;
using NHibernate.Driver;

namespace YesSql.Core.Data {
    public class MsSqlCeConfiguration : PersistenceConfiguration<MsSqlCeConfiguration> {
        protected MsSqlCeConfiguration() {
            Driver<SqlServerCeDriver>();
        }

        public static MsSqlCeConfiguration MsSqlCe40 {
            get { return new MsSqlCeConfiguration().Dialect<CustomMsSqlCe40Dialect>(); }
        }
    }

    public class CustomMsSqlCe40Dialect : MsSqlCe40Dialect
    {
        public override bool SupportsVariableLimit {
            get {
                return true;
            }
        }
    }
}