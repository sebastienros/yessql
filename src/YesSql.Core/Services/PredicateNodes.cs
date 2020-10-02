using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YesSql.Services
{
    public abstract class PredicateNode
    {
        public abstract void Build(StringBuilder builder);

        public abstract PredicateNode Clone();
    }

    public abstract class CompositeNode : PredicateNode
    {
        public List<PredicateNode> Children = new List<PredicateNode>();
    }

    public class AndNode : CompositeNode
    {
        public override void Build(StringBuilder builder)
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
            var clone = new AndNode();
            clone.Children = children;

            return clone;
        }
    }

    public class OrNode : CompositeNode
    {
        public override void Build(StringBuilder builder)
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
            var clone = new OrNode();
            clone.Children = children;

            return clone;
        }
    }

    public class FilterNode : CompositeNode
    {
        public FilterNode(string filter)
        {
            Filter = filter;
        }

        public string Filter;

        public override void Build(StringBuilder builder)
        {
            if (String.IsNullOrEmpty(Filter))
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
