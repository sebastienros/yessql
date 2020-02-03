using System;
using System.Text;

namespace YesSql.Collections
{
    public abstract class Collection : IDisposable
    {
        readonly IDisposable _scope;

        protected Collection()
        {
            _scope = CollectionHelper.EnterScope(this);
        }

        public abstract string GetSafeName();
        public static string CreateSafeName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var builder = new StringBuilder();
            var first = true;
            foreach (var c in name)
            {
                if (Char.IsLetter(c) || (!first && Char.IsDigit(c)))
                {
                    builder.Append(c);
                }

                first = false;
            }

            return builder.ToString();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }

    public static class CollectionExtensions
    {
        public static string GetPrefixedName(this Collection collection, string table)
        {
            var name = collection.GetSafeName();

            if (string.IsNullOrEmpty(name))
            {
                return table;
            }

            return string.Concat(name, "_", table);
        }
    }

    public class DefaultCollection : Collection
    {
        public override string GetSafeName()
        {
            return "";
        }
    }

    public class TypeCollection<T> : Collection
    {
        private readonly string _safeName;

        public TypeCollection()
        {
            _safeName = CreateSafeName(typeof(T).Name);

            if(String.IsNullOrEmpty(_safeName))
            {
                throw new ArgumentException("Invalid collection name: " + _safeName);
            }
        }

        public override string GetSafeName()
        {
            return _safeName;
        }
    }

    public class NamedCollection : Collection
    {
        private readonly string _safeName;

        public NamedCollection(string name)
        {
            _safeName = CreateSafeName(name);

            if (String.IsNullOrEmpty(_safeName))
            {
                throw new ArgumentException("Invalid collection name: " + _safeName);
            }
        }

        public override string GetSafeName()
        {
            return _safeName;
        }
    }
}
