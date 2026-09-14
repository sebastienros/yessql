using System;

namespace YesSql
{
    /// <summary>
    /// The exception that is thrown when the CLR type of a stored document cannot be resolved from
    /// its persisted type name.
    /// </summary>
    public class TypeResolutionException : Exception
    {
        /// <summary>
        /// Gets the persisted type name that could not be resolved.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeResolutionException"/> class.
        /// </summary>
        /// <param name="typeName">The persisted type name that could not be resolved.</param>
        public TypeResolutionException(string typeName) : base($"The type '{typeName}' could not be resolved. The assembly defining this type may not be loaded, or the type may have been renamed or removed. Register the type with 'IStore.TypeNames' so it can be resolved when its documents are loaded.")
        {
            TypeName = typeName;
        }
    }
}
