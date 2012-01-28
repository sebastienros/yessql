using System;
using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using YesSql.Core.Data.Models;
using YesSql.Core.Indexes;

namespace YesSql.Core.Data.Mappings {
    public class WhereTypeIsIndex : IAutoMappingAlteration {
        public void Alter(AutoPersistenceModel model) {
            model.Where(x => IsIndex(x) || IsDocument(x));
        }

        private static bool IsIndex(Type type)
        {
            return typeof (IIndex).IsAssignableFrom(type);
        }

        private static bool IsDocument(Type type) {
            return type == typeof(Document);
        }
    }
}
