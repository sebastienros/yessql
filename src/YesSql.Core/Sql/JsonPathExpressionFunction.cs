using System;

namespace YesSql.Sql
{
    public class JsonPathExpressionFunction : ISqlFunction
    {
        private readonly string _template;
        private readonly int[] _argumentIds;

        public JsonPathExpressionFunction(string template, params int[] argumentIds)
        {
            _template = template;
            _argumentIds = argumentIds;
        }

        public string Render(string[] arguments)
        {
            foreach (var id in _argumentIds)
            {
                arguments[id] = TransformPathExpression(arguments[id]);
            }

            return String.Format(_template, arguments);
        }

        private string TransformPathExpression(string jsonPathExpression)
        {
            return jsonPathExpression
                .Replace("$.", "")
                .Replace("[", ",")
                .Replace("].", ",")
                .Replace("]", "")
                .Replace(".", ",");
        }
    }
}
