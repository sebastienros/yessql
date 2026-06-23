using System;
using YesSql.Filters.Nodes;

namespace YesSql.Filters.Services
{
    /// <summary>
    /// Represents the configuration options associated with a term in a filter, including how it maps to and from a model.
    /// </summary>
    public abstract class TermOption
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TermOption"/> class.
        /// </summary>
        /// <param name="name">The name of the term these options apply to.</param>
        protected TermOption(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the term these options apply to.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Whether one or many of the specified term is allowed.
        /// </summary>
        public bool Single { get; set; } = true;

        /// <summary>
        /// Whether this term filter should always run, even when not specified.
        /// </summary>
        public bool AlwaysRun { get; set; }

        /// <summary>
        /// Gets or sets the delegate that maps the term value to a model.
        /// </summary>
        public Delegate MapTo { get; set; }
        /// <summary>
        /// Gets or sets the delegate that maps a value from a model back to the term.
        /// </summary>
        public Delegate MapFrom { get; set; }
        /// <summary>
        /// Gets or sets the factory that builds a <see cref="TermNode"/> from a term name and value when mapping from a model.
        /// </summary>
        public Func<string, string, TermNode> MapFromFactory { get; set; }
    }
}
