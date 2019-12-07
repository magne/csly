using System.Collections.Generic;

namespace expressionparser.model
{
    public class ExpressionContext
    {
        private readonly Dictionary<string, int> variables;

        public ExpressionContext()
        {
            variables = new Dictionary<string, int>();
        }

        public ExpressionContext(Dictionary<string, int> variables)
        {
            this.variables = variables;
        }

        public int? GetValue(string variable)
        {
            if (variables.ContainsKey(variable)) return variables[variable];
            return null;
        }
    }
}