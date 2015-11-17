using LightningDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YesSql.Core.Storage;

namespace YesSql.Storage.LightningDB
{
    public class LightningDocumentStorage : IDocumentStorage
    {
        public Dictionary<int, string> _documents = new Dictionary<int, string>();
        private readonly static JsonSerializerSettings _jsonSettings;
        private readonly LightningEnvironment _env;

        static LightningDocumentStorage()
        {
            _jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        }
        
        public LightningDocumentStorage(LightningEnvironment environment)
        {
            _env = environment;
        }

        public Task SaveAsync<T>(int id, T item)
        {
            var content = JsonConvert.SerializeObject(item, _jsonSettings);

            using (var tx = _env.BeginTransaction())
            using (var db = tx.OpenDatabase(null, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
            {
                tx.Put(db, Encoding.UTF8.GetBytes(id.ToString()), Encoding.UTF8.GetBytes(content));
                tx.Commit();
            }

            return Task.CompletedTask;
        }
        
        public Task DeleteAsync(int documentId)
        {
            using (var tx = _env.BeginTransaction())
            using (var db = tx.OpenDatabase(null, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
            {
                tx.Delete(db, Encoding.UTF8.GetBytes(documentId.ToString()));
                tx.Commit();
            }

            return Task.CompletedTask;
        }

        public async Task<IEnumerable<T>> GetAsync<T>(IEnumerable<int> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException("id");
            }

            var result = new List<T>();
            foreach (var id in ids)
            {
                using (var tx = _env.BeginTransaction(TransactionBeginFlags.ReadOnly))
                {
                    var db = tx.OpenDatabase();
                    var bytes = tx.Get(db, Encoding.UTF8.GetBytes(id.ToString()));

                    using (var binaryStream = new MemoryStream(bytes))
                    {
                        StringBuilder sb = new StringBuilder();

                        byte[] buffer = new byte[0x1000];
                        int numRead;
                        while ((numRead = await binaryStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            string text = Encoding.UTF8.GetString(buffer, 0, numRead);
                            sb.Append(text);
                        }

                        result.Add(JsonConvert.DeserializeObject<T>(sb.ToString(), _jsonSettings));
                    }
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetAsync(IEnumerable<int> ids)
        {
            return await GetAsync<object>(ids);
        }
        
    }
}
