using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Web.Script.Serialization;
using YesSql.Core.Data.Models;
using YesSql.Core.Services;

namespace YesSql.Core.Serialization
{
    public class JSonSerializerFactory : IDocumentSerializerFactory
    {
        private JSonSerializer _serializer;

        public IDocumentSerializer Build(IStore store)
        {
            if(_serializer == null)
            {
                _serializer = new JSonSerializer(store);
            }

            return _serializer;
        }
    }

    public class JSonSerializer : IDocumentSerializer
    {
        private readonly IStore _store;
        private readonly JavaScriptSerializer _serializer;

        public JSonSerializer(IStore store)
        {
            _store = store;
            _serializer = new JavaScriptSerializer();
        }

        public void Serialize(object obj, ref Document doc)
        {
            var objType = obj.GetType();
            doc.Content = _serializer.Serialize(obj);
            doc.Type = objType.IsAnonymousType() ? String.Empty : objType.SimplifiedTypeName();
        }

        public object Deserialize(Document doc)
        {
            // if a CLR type is specified, use it during deserialization
            if (!String.IsNullOrEmpty(doc.Type))
            {
                var type = Type.GetType(doc.Type, false);
                var des = _serializer.Deserialize(doc.Content, type);

                // if the document has an Id property, set it back
                _store.GetIdAccessor(type, "Id").Set(des, doc.Id);

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


    }
}
