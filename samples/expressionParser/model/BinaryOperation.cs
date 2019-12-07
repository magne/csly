namespace expressionparser.model
{
    public class BinaryOperation : IExpression
    {
        private readonly IExpression leftExpresion;
        private readonly ExpressionToken @operator;
        private readonly IExpression rightExpression;


        public BinaryOperation(IExpression left, ExpressionToken op, IExpression right)
        {
            leftExpresion = left;
            @operator = op;
            rightExpression = right;
        }

        public int? Evaluate(ExpressionContext context)
        {
            var left = leftExpresion.Evaluate(context);
            var right = rightExpression.Evaluate(context);

            if (left.HasValue && right.HasValue)
                switch (@operator)
                {
                    case ExpressionToken.PLUS:
                    {
                        return left.Value + right.Value;
                    }
                    case ExpressionToken.MINUS:
                    {
                        return left.Value - right.Value;
                    }
                    case ExpressionToken.TIMES:
                    {
                        return left.Value * right.Value;
                    }
                    case ExpressionToken.DIVIDE:
                    {
                        return left.Value / right.Value;
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