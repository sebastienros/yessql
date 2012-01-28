using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentNHibernate;
using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using YesSql.Core.Indexes;

namespace YesSql.Core.Data.Mappings
{
    /// <summary>
    /// Adds a relationship to Document
    /// </summary>
    public class IndexAlteration : IAutoMappingAlteration
    {
        private readonly IEnumerable<Type> _hasDocumentsIndexes;

        public IndexAlteration(ITypeSource typeSource)
        {
            var types = typeSource.GetTypes().ToArray();
            _hasDocumentsIndexes = types.Where(x => typeof (IHasDocumentsIndex).IsAssignableFrom(x));
        }

        public void Alter(AutoPersistenceModel model)
        {
            var methods = model.GetType().GetMethods();
            var method = methods.Single(x => x.Name == "Override" && x.GetGenericArguments().Any());
            foreach (var type in _hasDocumentsIndexes)
            {
                // model.Override<{TIndex}>()
                var genericMethod = method.MakeGenericMethod(type);

                var alterationType = typeof (AlterationHelper<>).MakeGenericType(type);
                var overrideMethod = alterationType.GetMethods().Single(x => x.Name == "Override");
                var alteration = overrideMethod.Invoke(null, null);

                genericMethod.Invoke(model, new[] {alteration});
            }
        }
    }

    internal class AlterationHelper<TIndex> where TIndex : IHasDocumentsIndex
    {
        public static Action<AutoMapping<TIndex>> Override()
        {
            return mapping => mapping.HasManyToMany(x => x.Documents)
                                  .AsSet()
                                  .Table("Documents_" + typeof (TIndex).Name)
                                  .Cascade.All()
                //.Inverse()
                ;

        }

    }
}

