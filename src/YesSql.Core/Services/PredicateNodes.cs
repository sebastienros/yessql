using System.Collections.Generic;
using System.Linq;
using YesSql.Utils;

namespace YesSql.Services
{
    internal abstract class PredicateNode
    {
        public abstract void Build(RentedStringBuilder builder);

        public abstract PredicateNode Clone();
    }

    internal abstract class CompositeNode : PredicateNode
    {
        public List<PredicateNode> Children = new();
    }

    internal sealed class AndNode : CompositeNode
    {
        public override void Build(RentedStringBuilder builder)
        {
            if (Children.Count > 0)
            {
                if (Children.Count == 1)
                {
                    Children[0].Build(builder);
                }
                else
                {
                    builder.Append(" (");

                    for (var i = 0; i < Children.Count; i++)
                    {
                        Children[i].Build(builder);

                        if (i < Children.Count - 1)
                        {
                            builder.Append(" AND ");
                        }
                    }

                    builder.Append(")");
                }
            }
        }

        public override PredicateNode Clone()
        {
            var children = Children.Select(x => x.Clone()).ToList();
            var clone = new AndNode
            {
                Children = children
            };

            return clone;
        }
    }

    internal sealed class OrNode : CompositeNode
    {
        public override void Build(RentedStringBuilder builder)
        {
            if (Children.Count > 0)
            {
                if (Children.Count == 1)
                {
                    Children[0].Build(builder);
                }
                else
                {
                    builder.Append(" (");

                    for (var i = 0; i < Children.Count; i++)
                    {
                        Children[i].Build(builder);

                        if (i < Children.Count - 1)
                        {
                            builder.Append(" OR");
                        }
                    }

                    builder.Append(")");
                }
            }
        }

        public override PredicateNode Clone()
        {
            var children = Children.Select(x => x.Clone()).ToList();
            var clone = new OrNode
            {
                Children = children
            };

            return clone;
        }
    }

    internal sealed class FilterNode : CompositeNode
    {
        public FilterNode(string filter)
        {
            Filter = filter;
        }

        public string Filter;

        public override void Build(RentedStringBuilder builder)
        {
            if (string.IsNullOrEmpty(Filter))
            {
                return;
            }

            builder.Append(Filter);
        }

        public override PredicateNode Clone()
        {
            return new FilterNode(Filter);
        }
    }
}
