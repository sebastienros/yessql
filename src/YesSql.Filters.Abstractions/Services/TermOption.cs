using System;
using YesSql.Filters.Abstractions.Nodes;

namespace YesSql.Filters.Abstractions.Services
{
    public abstract class TermOption
    {
        protected TermOption(string name)
        {
            Name = name;
        }

        public string Name { get; }

        /// <summary>
        /// Whether one or many of the specified term is allowed.
        /// </summary>
        public bool Single { get; set; } = true;

        /// <summary>
        /// Whether this term filter should always run, even when not specified.
        /// </summary>
        public bool AlwaysRun { get; set; }

        public Delegate MapTo { get; set; }
        public Delegate MapFrom { get; set; }
        public Func<string, string, TermNode> MapFromFactory { get; set; }
    }
}
