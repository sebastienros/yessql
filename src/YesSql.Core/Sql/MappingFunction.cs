using System;

namespace YesSql.Sql
{
    public class MappingFunction : ISqlFunction
    {
        private readonly string _name;

        public MappingFunction(string name)
        {
            _name = name;
        }

        public string Render(string[] arguments)
        {
            return _name + "(" + String.Join(", ", arguments) + ")";
        }
    }
}
