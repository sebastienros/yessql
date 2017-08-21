using System;
#if NET451
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif

namespace YesSql.Collections
{
    public class CollectionHelper
    {

#if !NET451
        private static readonly AsyncLocal<CollectionStack> _scopes = new AsyncLocal<CollectionStack>();

        internal static CollectionStack Scopes
        {
            get { return _scopes.Value; }
            set { _scopes.Value = value; }
        }
#else
        private static readonly string CollectionDataName = "Collection.Scopes" + AppDomain.CurrentDomain.Id;

        internal static CollectionStack Scopes
        {
            get
            {
                var handle = CallContext.LogicalGetData(CollectionDataName) as ObjectHandle;

                if (handle == null)
                {
                    return null;
                }

                return handle.Unwrap() as CollectionStack;
            }
            set
            {
                CallContext.LogicalSetData(CollectionDataName, new ObjectHandle(value));
            }
        }
#endif
        public static Collection Current
        {
            get
            {
                var scopes = GetOrCreateScopes();
                return scopes.Peek();
            }
        }

        internal static IDisposable EnterScope(Collection collection)
        {
            var scopes = GetOrCreateScopes();

            var scopeLease = new ScopeLease(scopes);
            Scopes = scopes.Push(collection);

            return scopeLease;
        }

        private static CollectionStack GetOrCreateScopes()
        {
            var scopes = Scopes;
            if (scopes == null)
            {
                scopes = CollectionStack.Empty;
                Scopes = scopes;
            }

            return scopes;
        }

        private sealed class ScopeLease : IDisposable
        {
            readonly CollectionStack _collectionStack;

            public ScopeLease(CollectionStack collectionStack)
            {
                _collectionStack = collectionStack;
            }

            public void Dispose()
            {
                Scopes = _collectionStack;
            }
        }
    }
}
