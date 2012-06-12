using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YesSql.Core.Data.Models;

namespace YesSql.Core.Serialization
{
    public class JSonNetSerializerFactory : IDocumentSerializerFactory
    {
        public IDocumentSerializer Build()
        {
            return new JSonNetSerializer();
        }
    }

    public class JSonNetSerializer : IDocumentSerializer
    {
    
        public void Serialize(object obj, ref Document doc)
        {
            if(obj == null)
            {
                throw new ArgumentNullException("obj");    
            }

            var objType = obj.GetType();
            doc.Content = JsonConvert.SerializeObject(obj, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            doc.Type = objType.IsAnonymousType() ? String.Empty : objType.SimplifiedTypeName();
        }

        public object Deserialize(Document doc)
        {
            if (String.IsNullOrEmpty(doc.Content)) 
            {
                return null;
            }

            // if a CLR type is specified, use it during deserialization
            if (!String.IsNullOrEmpty(doc.Type))
            {
                var type = Type.GetType(doc.Type, false);
                var des = JsonConvert.DeserializeObject(doc.Content, type, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

                return des;
            }

            return JObject.Parse(doc.Content);
        }
    }
}
