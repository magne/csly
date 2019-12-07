namespace expressionparser.model
{
    public class UnaryOperation : IExpression
    {
        private readonly ExpressionToken @operator;
        private readonly IExpression rightExpression;

        public UnaryOperation(ExpressionToken op, IExpression right)
        {
            @operator = op;
            rightExpression = right;
        }

        public int? Evaluate(ExpressionContext context)
        {
            var right = rightExpression.Evaluate(context);

            if (right.HasValue)
                switch (@operator)
                {
                    case ExpressionToken.PLUS:
                    {
                        return +right.Value;
                    }
                    case ExpressionToken.MINUS:
                    {
                        return -right.Value;
                    }
                    default:
                    {
                        return null;
                    }
                }
            return null;
        }
    }
}