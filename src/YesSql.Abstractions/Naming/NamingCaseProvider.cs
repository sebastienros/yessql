using System;
using System.Text.RegularExpressions;

namespace YesSql.Naming
{
    public class NamingCaseProvider
    {
        private readonly NamingCase _namingCase;

        public NamingCaseProvider(NamingCase namingCase)
        {
            _namingCase = namingCase;
        }
        public string GetName(string input)
        {
            switch (_namingCase)
            {
                case NamingCase.PascalCase:
                    return input;
                case NamingCase.SnakeCase:
                    return _snakeCase(input);
                case NamingCase.CamelCase:
                    return _camelCase(input);
                default:
                    throw new ArgumentOutOfRangeException(nameof(_namingCase), _namingCase, null);
            }
        }

        private static string _snakeCase(string input)
        {
            return Regex.Replace(
                Regex.Replace(
                    Regex.Replace(input, @"([\p{Lu}]+)([\p{Lu}][\p{Ll}])", "$1_$2"), @"([\p{Ll}\d])([\p{Lu}])", "$1_$2"), @"[-\s]", "_").ToLower();
        }

        /// <summary>
        /// Make the first letter of the string a lowercase
        /// </summary>
        private static string _camelCase(string input)
        {
            return input.Length > 0 ? input.Substring(0, 1).ToLower() + input.Substring(1) : input;
        }
    }
}
