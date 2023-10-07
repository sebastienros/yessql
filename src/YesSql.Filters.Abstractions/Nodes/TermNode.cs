using YesSql.Filters.Services;

namespace YesSql.Filters.Nodes
{
    public abstract class TermNode : FilterNode
    {
        protected TermNode(string termName)
        {
            TermName = termName;
        }

        public string TermName { get; }
        public override TResult Accept<TArgument, TResult>(IFilterVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.Visit(this, argument);
    }
}
