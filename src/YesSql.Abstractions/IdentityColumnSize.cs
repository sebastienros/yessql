namespace YesSql
{
    /// <summary>
    /// Defines the size of an identity column.
    /// </summary>
    public enum IdentityColumnSize
    {
        /// <summary>
        /// A 32-bit (<see cref="int"/>) identity column.
        /// </summary>
        Int32 = 1,

        /// <summary>
        /// A 64-bit (<see cref="long"/>) identity column.
        /// </summary>
        Int64 = 2
    }
}
