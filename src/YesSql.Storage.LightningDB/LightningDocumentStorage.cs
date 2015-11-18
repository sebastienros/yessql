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
    public class LightningDocumentStorage : IDocumentStorage, IDisposable
    {
        public Dictionary<int, string> _documents = new Dictionary<int, string>();
        private readonly static JsonSerializerSettings _jsonSettings;
        private readonly LightningEnvironment _env;

        static LightningDocumentStorage()
        {
            _jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
        }

        public LightningDocumentStorage(LightningEnvironment environment)
        {
            _env = environment;
        }

        public Task CreateAsync<T>(int[] ids, T[] items)
        {
            using (var tx = _env.BeginTransaction())
            {
                using (var db = tx.OpenDatabase())
                {
                    for (int i = 0; i < ids.Length; i++)
                    {
                        var content = JsonConvert.SerializeObject(items[i], _jsonSettings);
                        tx.Put(db, Encoding.UTF8.GetBytes(ids[i].ToString()), Encoding.UTF8.GetBytes(content));
                    }

                    tx.Commit();
                }
            }

            return Task.CompletedTask;
        }

        public Task UpdateAsync<T>(int[] ids, T[] items)
        {
            return CreateAsync(ids, items);
        }

        public Task DeleteAsync(int[] ids)
        {
            using (var tx = _env.BeginTransaction())
            {
                using (var db = tx.OpenDatabase())
                {
                    for (int i = 0; i < ids.Length; i++)
                    {
                        tx.Delete(db, Encoding.UTF8.GetBytes(ids[i].ToString()));
                    }

                    tx.Commit();
                }
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
            using (var tx = _env.BeginTransaction())
            {
                using (var db = tx.OpenDatabase())
                {
                    foreach (var id in ids)
                    {
                        var bytes = tx.Get(db, Encoding.UTF8.GetBytes(id.ToString()));

                        if (bytes == null || bytes.Length == 0)
                        {
                            continue;
                        }

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
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetAsync(IEnumerable<int> ids)
        {
            return await GetAsync<object>(ids);
        }
        
        public void Dispose()
        {
        }
    }
}
