using System;
using System.Text;

namespace YesSql.Collections
{
    public abstract class Collection : IDisposable
    {
        IDisposable _scope;

        public Collection()
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
            bool first = true;
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

            return CollectionHelper.GetPrefixedName(name, table);
        }
    }

    public class DefaultCollection : Collection
    {
        public DefaultCollection()
        {

        }

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
