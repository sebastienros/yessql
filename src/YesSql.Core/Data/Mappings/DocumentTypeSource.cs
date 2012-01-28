using System;
using System.Collections.Generic;
using System.Linq;
using FluentNHibernate;
using FluentNHibernate.Diagnostics;
using YesSql.Core.Data.Models;
using YesSql.Core.Indexes;

namespace YesSql.Core.Data.Mappings
{
    /// <summary>
    /// An <see cref="ITypeSource"/> implementation declaring <see cref="Document"/> as 
    /// a mapped class
    /// </summary>
    public class DocumentTypeSource : ITypeSource
    {
        public IEnumerable<Type> GetTypes()
        {
            yield return typeof (Document);
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
