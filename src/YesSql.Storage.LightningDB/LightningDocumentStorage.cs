using LightningDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YesSql.Core.Storage;
using YesSql.Core.Services;

namespace YesSql.Storage.LightningDB
{
    public class LightningDocumentStorage : IDocumentStorage, IDisposable
    {
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

        public ISession Session { get; set; }

        public Task CreateAsync(params IIdentityEntity[] documents)
        {
            using (var tx = _env.BeginTransaction())
            {
                using (var db = tx.OpenDatabase())
                {
                    foreach (var document in documents)
                    {
                        var content = JsonConvert.SerializeObject(document.Entity, _jsonSettings);
                        tx.Put(db, Encoding.UTF8.GetBytes(document.Id.ToString()), Encoding.UTF8.GetBytes(content));
                    }

                    tx.Commit();
                }
            }

#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        public Task UpdateAsync(params IIdentityEntity[] documents)
        {
            return CreateAsync(documents);
        }

        public Task DeleteAsync(params IIdentityEntity[] documents)
        {
            using (var tx = _env.BeginTransaction())
            {
                using (var db = tx.OpenDatabase())
                {
                    foreach (var document in documents)
                    {
                        tx.Delete(db, Encoding.UTF8.GetBytes(document.Id.ToString()));
                    }

                    tx.Commit();
                }
            }

#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        public async Task<IEnumerable<T>> GetAsync<T>(params int[] ids)
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

        public async Task<IEnumerable<object>> GetAsync(params IIdentityEntity[] documents)
        {
            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            var result = new List<object>();
            using (var tx = _env.BeginTransaction())
            {
                using (var db = tx.OpenDatabase())
                {
                    foreach (var document in documents)
                    {
                        var bytes = tx.Get(db, Encoding.UTF8.GetBytes(document.Id.ToString()));

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

                            result.Add(JsonConvert.DeserializeObject(sb.ToString(), document.EntityType, _jsonSettings));
                        }
                    }
                }
            }

            return result;
        }

        public void Dispose()
        {
        }
    }
}
