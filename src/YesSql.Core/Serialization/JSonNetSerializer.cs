using System;
using System.IO;
using Newtonsoft.Json;
using YesSql.Core.Data.Models;
using YesSql.Core.Services;

namespace YesSql.Core.Serialization
{
    public class JSonNetSerializerFactory : IDocumentSerializerFactory
    {
        public IDocumentSerializer Build(IStore store)
        {
            return new JSonNetSerializer(store);
        }
    }

    public class JSonNetSerializer : IDocumentSerializer
    {
        private readonly IStore _store;
        private readonly JsonSerializer _serializer;

        public JSonNetSerializer(IStore store)
        {
            _store = store;
            _serializer = new JsonSerializer();
        }

        public void Serialize(object obj, ref Document doc)
        {
            var objType = obj.GetType();
            using (var sw = new StringWriter())
            {
                _serializer.Serialize(sw, obj);
                doc.Content = _serializer.ToString();
            }

            doc.Type = objType.IsAnonymousType() ? String.Empty : objType.SimplifiedTypeName();
        }

        public object Deserialize(Document doc)
        {
            // if a CLR type is specified, use it during deserialization
            if (!String.IsNullOrEmpty(doc.Type))
            {
                var type = Type.GetType(doc.Type, false);
                var des = JsonConvert.DeserializeObject(doc.Content, type);

                // if the document has an Id property, set it back
                _store.GetIdAccessor(type, "Id").Set(des, doc.Id);

                return des;
            }

            var obj = JsonConvert.DeserializeObject(doc.Content);
            // var obj = _serializer.DeserializeObject(doc.Content) as IDictionary<String, object>;

            if (obj == null)
            {
                throw new InvalidCastException("Could not convert serialized object");
            }

            return obj;
        }
    }
}
