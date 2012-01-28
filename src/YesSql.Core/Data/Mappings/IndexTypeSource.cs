using System;
using System.Collections.Generic;
using System.Linq;
using FluentNHibernate;
using FluentNHibernate.Diagnostics;
using YesSql.Core.Indexes;

namespace YesSql.Core.Data.Mappings
{
    /// <summary>
    /// the IIndex classes are lookep up in the same assemblies as registered index descriptors
    /// base classes are ignored so they don't get a table too
    /// </summary>
    public class IndexTypeSource : ITypeSource
    {
        private readonly IEnumerable<IIndexProvider> _indexes;

        public IndexTypeSource(IEnumerable<IIndexProvider> indexes)
        {
            _indexes = indexes;
        }

        public IEnumerable<Type> GetTypes()
        {
            return _indexes
                .Select(x => x.GetType().Assembly).Distinct()
                .SelectMany(x => x.GetExportedTypes())
                .Where(x => typeof (IIndex).IsAssignableFrom(x));
        }

        public void LogSource(IDiagnosticLogger logger)
        {

        }

        public string GetIdentifier()
        {
            return GetType().Name;
        }
    }
}
