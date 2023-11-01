using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using YesSql.Filters.Nodes;

namespace YesSql.Filters.Services
{
    public abstract class FilterResult<T, TTermOption> : IEnumerable<TermNode> where TTermOption : TermOption
    {

        protected Dictionary<string, TermNode> _terms = new Dictionary<string, TermNode>(StringComparer.OrdinalIgnoreCase);

        protected FilterResult(IReadOnlyDictionary<string, TTermOption> termOptions)
        {
            TermOptions = termOptions;
        }

        protected FilterResult(List<TermNode> terms, IReadOnlyDictionary<string, TTermOption> termOptions)
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
            => $"{String.Join(" ", _terms.Values.Select(s => s.ToNormalizedString()))}";

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

        public IEnumerator<TermNode> GetEnumerator()
            => _terms.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _terms.Values.GetEnumerator();
    }
}
