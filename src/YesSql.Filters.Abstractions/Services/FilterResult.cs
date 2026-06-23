using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using YesSql.Filters.Nodes;

namespace YesSql.Filters.Services
{
    /// <summary>
    /// Represents the result of parsing a filter expression as a collection of <see cref="TermNode"/> instances, providing mapping and serialization helpers.
    /// </summary>
    /// <typeparam name="T">The type the filter is applied to.</typeparam>
    /// <typeparam name="TTermOption">The type of the term options.</typeparam>
    public abstract class FilterResult<T, TTermOption> : IEnumerable<TermNode> where TTermOption : TermOption
    {

        /// <summary>
        /// The parsed terms keyed by their term name, compared case-insensitively.
        /// </summary>
        protected Dictionary<string, TermNode> _terms = new Dictionary<string, TermNode>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterResult{T, TTermOption}"/> class with the specified term options.
        /// </summary>
        /// <param name="termOptions">The configured options keyed by term name.</param>
        protected FilterResult(IReadOnlyDictionary<string, TTermOption> termOptions)
        {
            TermOptions = termOptions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterResult{T, TTermOption}"/> class from a set of parsed terms.
        /// </summary>
        /// <param name="terms">The parsed terms to add to the result.</param>
        /// <param name="termOptions">The configured options keyed by term name.</param>
        protected FilterResult(IReadOnlyList<TermNode> terms, IReadOnlyDictionary<string, TTermOption> termOptions)
        {
            TermOptions = termOptions;

            foreach (var term in terms)
            {
                TryAddOrReplace(term);
            }
        }

        /// <summary>
        /// The configured options for a <see cref="TermNode"/>
        /// </summary>
        public IReadOnlyDictionary<string, TTermOption> TermOptions { get; }

        /// <summary>
        /// Applies registered mappings to a model.
        /// <typeparam name="TModel">The type to map to.</typeparam>
        /// </summary>
        public void MapTo<TModel>(TModel model)
        {
            foreach (var term in _terms.Values)
            {
                var option = TermOptions[term.TermName];

                if (option.MapTo is Action<string, TModel> action &&
                    term is TermOperationNode { Operation: UnaryNode node })
                {
                    action(node.Value, model);
                }
            }
        }

        /// <summary>
        /// Returns a normalized query string, applying any inferred boolean logic and parenthesis
        /// </summary>
        public string ToNormalizedString()
            => $"{string.Join(" ", _terms.Values.Select(s => s.ToNormalizedString()))}";

        /// <summary>
        /// Returns the filter terms.
        /// </summary>
        public override string ToString()
            => $"{string.Join(" ", _terms.Values.Select(s => s.ToString()))}";

        /// <summary>
        /// Adds or replaces a <see cref="TermNode"/>
        /// </summary>
        public bool TryAddOrReplace(TermNode term)
        {
            // Check the term options 
            if (!TermOptions.TryGetValue(term.TermName, out var termOption))
            {
                return false;
            }

            if (_terms.TryGetValue(term.TermName, out var existingTerm))
            {
                if (termOption.Single)
                {
                    // Replace
                    _terms[term.TermName] = term;
                    return true;
                }

                // Add
                if (existingTerm is CompoundTermNode compound)
                {
                    compound.Children.Add(term as TermOperationNode);
                }
                else
                {
                    var newCompound = new AndTermNode(existingTerm as TermOperationNode, term as TermOperationNode);
                    _terms[term.TermName] = newCompound;
                    return true;
                }
            }

            _terms[term.TermName] = term;

            return true;
        }

        /// <summary>
        /// Removes a term from the query
        /// </summary>
        public bool TryRemove(string name)
            => _terms.Remove(name);        

        /// <summary>
        /// Returns an enumerator that iterates through the parsed terms.
        /// </summary>
        /// <returns>An enumerator for the parsed terms.</returns>
        public IEnumerator<TermNode> GetEnumerator()
            => _terms.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _terms.Values.GetEnumerator();
    }
}
