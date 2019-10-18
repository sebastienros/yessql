using System;

namespace YesSql
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException() : base("The document could not be updated as it has been changed by another process.")
        {
        }
    }
}
