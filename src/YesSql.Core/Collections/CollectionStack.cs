using System;

namespace YesSql.Collections
{
    internal class CollectionStack
    {
        private readonly CollectionStack _previous;
        private readonly Collection _collection;

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

        public static readonly CollectionStack Empty = new CollectionStack();

        public CollectionStack Push(Collection c)
        {
            return new CollectionStack(this, c);
        }

        public Collection Peek()
        {
            return _collection ?? new DefaultCollection();
        }
    }
}
