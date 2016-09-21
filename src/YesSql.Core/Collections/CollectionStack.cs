using System;

namespace YesSql.Core.Collections
{
    internal class CollectionStack
    {
        private readonly CollectionStack _previous;
        private readonly Collection _collection;

        static CollectionStack()
        {
            Empty = new CollectionStack().Push(new DefaultCollection());
        }

        private CollectionStack()
        {
        }

        private CollectionStack(CollectionStack previous, Collection collection)
        {
            if (previous == null)
            {
                throw new ArgumentNullException(nameof(previous));
            }

            _previous = previous;
            _collection = collection;
        }

        public readonly static CollectionStack Empty;

        public CollectionStack Push(Collection c)
        {
            return new CollectionStack(this, c);
        }

        public Collection Peek()
        {
            return _collection;
        }
    }
}
