using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YesSql.Core.Storage;

namespace YesSql.Storage.FileSystem
{
    public class FileSystemDocumentStorage : IDocumentStorage
    {
        public Dictionary<int, string> _documents = new Dictionary<int, string>();
        private readonly static JsonSerializerSettings _jsonSettings;
        private readonly string _root;

        static FileSystemDocumentStorage()
        {
            _jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
        }

        public void Clear()
        {
            Directory.Delete(_root, true);
        }

        public FileSystemDocumentStorage(string root)
        {
            _root = root;
            if(!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
        }

        private string GetFilePath(int id)
        {
            return Path.Combine(_root, id.ToString()) + ".json";
        }

        public async Task CreateAsync<T>(int[] ids, T[] items)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                var fileName = GetFilePath(ids[i]);
                var content = JsonConvert.SerializeObject(items[i], _jsonSettings);
                await WriteTextAsync(fileName, content);
            }
        }

        public Task UpdateAsync<T>(int[] ids, T[] items)
        {
            return CreateAsync(ids, items);
        }

        public Task DeleteAsync(int[] ids)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                var fileName = GetFilePath(ids[i]);
                File.Delete(fileName);
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
                var fileName = GetFilePath(id);

                if (File.Exists(fileName))
                {
                    var content = await ReadTextAsync(fileName);
                    result.Add(JsonConvert.DeserializeObject<T>(content, _jsonSettings));
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetAsync(IEnumerable<int> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException("id");
            }

            var result = new List<object>();
            foreach (var id in ids)
            {
                var fileName = GetFilePath(id);

                if (File.Exists(fileName))
                {
                    var content = await ReadTextAsync(fileName);
                    result.Add(JsonConvert.DeserializeObject(content, _jsonSettings));
                }
            }

            return result;
        }

        static async Task WriteTextAsync(string filePath, string text)
        {
            byte[] encodedText = Encoding.UTF8.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }

        static async Task<string> ReadTextAsync(string filePath)
        {
            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true))
            {
                StringBuilder sb = new StringBuilder();

                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string text = Encoding.UTF8.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }

                return sb.ToString();
            }
        }

    }
}
