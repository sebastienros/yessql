using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using FluentNHibernate;
using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using YesSql.Core.Data.Models;
using YesSql.Core.Indexes;

namespace YesSql.Core.Data.Mappings
{
    /// <summary>
    /// Adds a relationship to IHasDocumentsIndex by the name of the class
    /// It is needed in order to apply .Cascade.AllDeleteOrphan() to the mapping
    /// and delete all related reduced indexes when deleting a Document
    /// </summary>
    public class DocumentAlteration : IAutoMappingAlteration
    {
        private readonly IEnumerable<Type> _hasDocumentIndexes;
        private readonly IEnumerable<Type> _hasDocumentsIndexes;

        public DocumentAlteration(ITypeSource typeSource)
        {
            var types = typeSource.GetTypes().ToArray();
            _hasDocumentIndexes = types.Where(x => typeof (IHasDocumentIndex).IsAssignableFrom(x));
            _hasDocumentsIndexes = types.Where(x => typeof (IHasDocumentsIndex).IsAssignableFrom(x));
        }

        public void Alter(AutoPersistenceModel model)
        {
            model.Override<Document>(mapping =>
            {
                // todo: study the gains on using HiLo
                // mapping.Id(x => x.Id).GeneratedBy.HiLo("100");

                mapping.Map(x => x.Type).Index("idx_document_type");
                mapping.Map(x => x.Content).Length(4001);

                foreach (var index in _hasDocumentsIndexes)
                {
                    var type = typeof (HasManyAlteration<>).MakeGenericType(index);
                    var alteration = (IAlteration) Activator.CreateInstance(type);
                    alteration.Override(mapping);
                }

                foreach (var index in _hasDocumentIndexes)
                {
                    var type = typeof (HasOneAlteration<>).MakeGenericType(index);
                    var alteration = (IAlteration) Activator.CreateInstance(type);
                    alteration.Override(mapping);
                }
            });
        }
    }

    internal interface IAlteration
    {
        void Override(AutoMapping<Document> mapping);
    }

    /// <summary>
    /// Add a "fake" column to the automapping record so that the column can be
    /// referenced when building joins accross document and indexes.
    /// </summary>
    internal class HasManyAlteration<TIndex> : IAlteration
    {
        public void Override(AutoMapping<Document> mapping)
        {
            // mapping.HasManyToMany() Func<Document, IEnumerable<TChild>> memberExpression

            // public IList<{TIndex}> {TIndex} {get;set;}
            var name = typeof (TIndex).Name;
            var dynamicMethod = new DynamicMethod(name, typeof (IEnumerable<TIndex>), null, typeof (Document));
            var syntheticMethod = new SyntheticMethodInfo(dynamicMethod, typeof (Document));
            var syntheticProperty = new SyntheticPropertyInfo(syntheticMethod);

            // doc => doc.{TIndex}
            var parameter = Expression.Parameter(typeof (Document), "doc");
            var syntheticExpression = (Expression<Func<Document, IEnumerable<TIndex>>>) Expression.Lambda(
                typeof (Func<Document, IEnumerable<TIndex>>),
                Expression.Property(parameter, syntheticProperty),
                parameter);

            //mapping.HasManyToMany(Reveal.Member<Document, IEnumerable<TIndex>>(typeof(TIndex).Name))
            mapping.HasManyToMany(syntheticExpression)
                .AsSet()
                // prevent NHibernate from trying to access the property (it's fake)
                .Access.NoOp()
                .LazyLoad()
                .Table("Documents_" + name)
                .Cascade.All()
                .Inverse()
                ;
        }
    }

    /// <summary>
    /// Add a "fake" column to the automapping record so that the column can be
    /// referenced when building joins accross document and indexes.
    /// </summary>
    internal class HasOneAlteration<TIndex> : IAlteration
    {
        public void Override(AutoMapping<Document> mapping)
        {
            // mapping.HasMany() Func<Document, IEnumerable<TChild>> memberExpression

            // public IList<{TIndex}> {TIndex} { get; set; }
            var name = typeof (TIndex).Name;
            var dynamicMethod = new DynamicMethod(name, typeof (IEnumerable<TIndex>), null, typeof (Document));
            var syntheticMethod = new SyntheticMethodInfo(dynamicMethod, typeof (Document));
            var syntheticProperty = new SyntheticPropertyInfo(syntheticMethod);

            // doc => doc.{TIndex}
            var parameter = Expression.Parameter(typeof (Document), "doc");
            var syntheticExpression = (Expression<Func<Document, IEnumerable<TIndex>>>) Expression.Lambda(
                typeof (Func<Document, IEnumerable<TIndex>>),
                Expression.Property(parameter, syntheticProperty),
                parameter);

            mapping.HasMany(syntheticExpression)
                .KeyColumn("Document_id")
                // prevent NHibernate from trying to access the property (it's fake)
                .Access.NoOp()
                .ForeignKeyCascadeOnDelete()
                .Inverse()
                ;
        }
    }

    /// <summary>
    /// Synthetic method around a dynamic method. We need this so that we can
    /// override the "static" method attributes, and also return a valid "DeclaringType".
    /// </summary>
    public class SyntheticMethodInfo : MethodInfo
    {
        private readonly DynamicMethod _dynamicMethod;
        private readonly Type _declaringType;

        public SyntheticMethodInfo(DynamicMethod dynamicMethod, Type declaringType)
        {
            if (dynamicMethod == null)
            {
                throw new ArgumentNullException("dynamicMethod");
            }

            if (declaringType == null)
            {
                throw new ArgumentNullException("declaringType");
            }

            _dynamicMethod = dynamicMethod;
            _declaringType = declaringType;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return _dynamicMethod.GetCustomAttributes(inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return _dynamicMethod.IsDefined(attributeType, inherit);
        }

        public override ParameterInfo[] GetParameters()
        {
            return _dynamicMethod.GetParameters();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return _dynamicMethod.GetMethodImplementationFlags();
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters,
                                      CultureInfo culture)
        {
            return _dynamicMethod.Invoke(obj, invokeAttr, binder, parameters, culture);
        }

        public override MethodInfo GetBaseDefinition()
        {
            return _dynamicMethod.GetBaseDefinition();
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get { return _dynamicMethod.ReturnTypeCustomAttributes; }
        }

        public override string Name
        {
            get { return _dynamicMethod.Name; }
        }

        public override Type DeclaringType
        {
            get { return _declaringType; }
        }

        public override Type ReflectedType
        {
            get { return _dynamicMethod.ReflectedType; }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { return _dynamicMethod.MethodHandle; }
        }

        public override MethodAttributes Attributes
        {
            get { return _dynamicMethod.Attributes & ~MethodAttributes.Static; }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return _dynamicMethod.GetCustomAttributes(attributeType, inherit);
        }

        public override Type ReturnType
        {
            get { return _dynamicMethod.ReturnType; }
        }
    }

    /// <summary>
    /// Synthetic property around a method info (the "getter" method).
    /// This is a minimal implementation enabling support for AutoMapping.References.
    /// </summary>
    public class SyntheticPropertyInfo : PropertyInfo
    {
        private readonly MethodInfo _getMethod;

        public SyntheticPropertyInfo(MethodInfo getMethod)
        {
            _getMethod = getMethod;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index,
                                        CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index,
                                      CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            return _getMethod;
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            return null;
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            throw new NotImplementedException();
        }

        public override string Name
        {
            get { return _getMethod.Name; }
        }

        public override Type DeclaringType
        {
            get { return _getMethod.DeclaringType; }
        }

        public override Type ReflectedType
        {
            get { return _getMethod.ReflectedType; }
        }

        public override Type PropertyType
        {
            get { return _getMethod.ReturnType; }
        }

        public override PropertyAttributes Attributes
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override int MetadataToken
        {
            get { return 0; }
        }

        public override Module Module
        {
            get { return null; }
        }

        public override MemberTypes MemberType
        {
            get { return MemberTypes.Property; }
        }
    }
}

