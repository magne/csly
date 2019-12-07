namespace expressionparser.model
{
    public class Group : IExpression
    {
        private readonly IExpression innerExpression;

        public Group(IExpression expr)
        {
            innerExpression = expr;
        }

        public int? Evaluate(ExpressionContext context)
        {
            return innerExpression.Evaluate(context);
        }
    }
}