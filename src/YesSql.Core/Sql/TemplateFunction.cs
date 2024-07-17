using System;

namespace YesSql.Sql
{
    public class TemplateFunction : ISqlFunction
    {
        private readonly string _template;

        public TemplateFunction(string template) => _template = template;

        public string Render(string[] arguments) => string.Format(_template, arguments);
    }
}
