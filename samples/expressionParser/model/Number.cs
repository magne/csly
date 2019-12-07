namespace expressionparser.model
{
    public sealed class Number : IExpression
    {
        private readonly int value;

        public Number(int value)
        {
            this.value = value;
        }

        public int? Evaluate(ExpressionContext context)
        {
            return value;
        }
    }
}