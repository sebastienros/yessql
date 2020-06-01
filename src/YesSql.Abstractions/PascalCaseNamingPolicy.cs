using System;
using System.Linq;

namespace YesSql
{
    public class PascalCaseNamingPolicy : NamingPolicy
    {
        private static readonly char[] _separator = new char[] { ' ' };

        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("The name can't be null or empty.", nameof(name));
            }

            return string.Concat(name
                .Split(_separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Substring(0, 1).ToUpper() + w.Substring(1)));
        }
    }
}
