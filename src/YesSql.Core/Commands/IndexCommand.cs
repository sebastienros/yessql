using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YesSql.Core.Indexes;

namespace YesSql.Core.Commands
{
    public abstract class IndexCommand : IIndexCommand
    {
        protected readonly string _tablePrefix;

        private static readonly ConcurrentDictionary<RuntimeTypeHandle, PropertyInfo[]> TypeProperties = new ConcurrentDictionary<RuntimeTypeHandle, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> InsertsList = new ConcurrentDictionary<RuntimeTypeHandle, string>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> UpdatesList = new ConcurrentDictionary<RuntimeTypeHandle, string>();

        private static PropertyInfo ReduceIndexAddedDocumentPropertyInfo = typeof(ReduceIndex).GetProperty("RemovedDocuments");
        private static PropertyInfo ReduceIndexRemovedDocumentPropertyInfo = typeof(ReduceIndex).GetProperty("AddedDocuments");
        private static PropertyInfo IndexIdPropertyInfo = typeof(IIndex).GetProperty("Id");

        protected static PropertyInfo[] KeysProperties = new[] { typeof(IIndex).GetProperty("Id") };

        public abstract int ExecutionOrder { get; }

        public IndexCommand(IIndex index, string tablePrefix)
        {
            Index = index;
            _tablePrefix = tablePrefix;
        }

        public IIndex Index { get; }
        public Document Document { get; }

        public abstract Task ExecuteAsync(DbConnection connection, DbTransaction transaction);

        protected static PropertyInfo[] TypePropertiesCache(Type type)
        {
            PropertyInfo[] pis;
            if (TypeProperties.TryGetValue(type.TypeHandle, out pis))
            {
                return pis;
            }

            var properties = type.GetProperties().Where(IsWriteable).ToArray();
            TypeProperties[type.TypeHandle] = properties;
            return properties;
        }

        protected string Inserts(Type type)
        {
            string result;
            if (InsertsList.TryGetValue(type.TypeHandle, out result))
            {
                return result;
            }

            string values = "DEFAULT VALUES";

            var allProperties = TypePropertiesCache(type);

            if (allProperties.Any())
            {
                var sbColumnList = new StringBuilder(null);

                for (var i = 0; i < allProperties.Count(); i++)
                {
                    var property = allProperties.ElementAt(i);
                    sbColumnList.Append($"[{property.Name}]");
                    if (i < allProperties.Count() - 1)
                        sbColumnList.Append(", ");
                }

                var sbParameterList = new StringBuilder(null);
                for (var i = 0; i < allProperties.Count(); i++)
                {
                    var property = allProperties.ElementAt(i);
                    sbParameterList.Append($"@{property.Name}");
                    if (i < allProperties.Count() - 1)
                        sbParameterList.Append(", ");
                }

                values = $"({sbColumnList}) values ({sbParameterList})";
            }

            InsertsList[type.TypeHandle] = result = $"insert into [{_tablePrefix}{type.Name}] {values};";
            return result;
        }

        protected string Updates(Type type)
        {
            string result;
            if (UpdatesList.TryGetValue(type.TypeHandle, out result))
            {
                return result;
            }

            var allProperties = TypePropertiesCache(type);
            var values = new StringBuilder(null);

            for (var i = 0; i < allProperties.Length; i++)
            {
                var property = allProperties[i];
                values.Append($"[{property.Name}]=@{property.Name}");
                if (i < allProperties.Length - 1)
                    values.Append(", ");
            }

            UpdatesList[type.TypeHandle] = result = $"update [{_tablePrefix}{type.Name}] set {values} where Id = @Id;";
            return result;
        }

        private static bool IsWriteable(PropertyInfo pi)
        {
            return
                //pi.Name != ReduceIndexAddedDocumentPropertyInfo.Name &&
                //pi.Name != ReduceIndexRemovedDocumentPropertyInfo.Name &&
                pi.Name != IndexIdPropertyInfo.Name
                ;
        }
    }
}
