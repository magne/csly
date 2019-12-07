namespace expressionparser.model
{
    public interface IExpression
    {
        int? Evaluate(ExpressionContext context);
    }
}