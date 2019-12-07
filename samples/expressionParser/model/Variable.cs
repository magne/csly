namespace expressionparser.model
{
    public sealed class Variable : IExpression
    {
        private readonly string variableName;

        public Variable(string varName)
        {
            variableName = varName;
        }


        public int? Evaluate(ExpressionContext context)
        {
            return context.GetValue(variableName);
        }
    }
}