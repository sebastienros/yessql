using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web.Script.Serialization;
using YesSql.Core.Data.Models;
using YesSql.Core.Services;

namespace YesSql.Core.Serialization
{
    public class JSonSerializer : IDocumentSerializer
    {
        private readonly JavaScriptSerializer _serializer;

        public JSonSerializer()
        {
            _serializer = new JavaScriptSerializer();
        }

        public void Serialize(object obj, ref Document doc)
        {
            var objType = obj.GetType();
            doc.Content = _serializer.Serialize(obj);
            doc.Type = IsAnonymousType(objType) ? String.Empty : objType.SimplifiedTypeName();
        }

        public object Deserialize(Document doc)
        {
            // if a CLR type is specified, use it during deserialization
            if (!String.IsNullOrEmpty(doc.Type))
            {
                var type = Type.GetType(doc.Type, false);
                var des = _serializer.Deserialize(doc.Content, type);

                // if the document has an Id property, set it back
                var idInfo = des.GetType().GetProperty("Id");
                if (idInfo != null) 
                {
                    idInfo.SetValue(des, doc.Id, null);
                }

                return des;
            }

            var obj = _serializer.DeserializeObject(doc.Content) as IDictionary<String, object>;

            if (obj == null)
            {
                throw new InvalidCastException("Could not convert serialized object");
            }

            return ConvertToDynamic(obj);
        }

        private static object ConvertToDynamic(IEnumerable<KeyValuePair<string, object>> obj)
        {
            var dyn = new ExpandoObject() as IDictionary<String, object>;
            foreach (var pair in obj)
            {
                if (pair.Value is IEnumerable<KeyValuePair<string, object>>)
                {
                    dyn[pair.Key] = ConvertToDynamic((IEnumerable<KeyValuePair<string, object>>) pair.Value);
                }
                else
                {
                    dyn[pair.Key] = pair.Value;
                }
            }

            return dyn;
        }

        public static bool IsAnonymousType(Type type)
        {
            return Attribute.IsDefined(type, typeof (CompilerGeneratedAttribute), false)
                   && type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }
    }
}
