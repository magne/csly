using expressionparser.model;
using sly.lexer;
using sly.parser.generator;

namespace expressionparser
{
    public class VariableExpressionParser
    {
        [Production("primary: INT")]
        public IExpression PrimaryNumber(Token<ExpressionToken> intToken)
        {
            return new Number(intToken.IntValue);
        }

        [Production("primary: IDENTIFIER")]
        public IExpression PrimaryIdentifier(Token<ExpressionToken> idToken)
        {
            return new Variable(idToken.StringWithoutQuotes);
        }

        [Production("primary: LPAREN expression RPAREN")]
        public IExpression Group(object discaredLParen, IExpression groupValue, object discardedRParen)
        {
            return new Group(groupValue);
        }


        [Production("expression : term PLUS expression")]
        [Production("expression : term MINUS expression")]
        public IExpression Expression(IExpression left, Token<ExpressionToken> operatorToken, IExpression right)
        {
            return new BinaryOperation(left, operatorToken.TokenID, right);
        }

        [Production("expression : term")]
        public IExpression Expression_Term(IExpression termValue)
        {
            return termValue;
        }

        [Production("term : factor TIMES term")]
        [Production("term : factor DIVIDE term")]
        public IExpression Term(IExpression left, Token<ExpressionToken> operatorToken, IExpression right)
        {
            return new BinaryOperation(left, operatorToken.TokenID, right);
        }

        [Production("term : factor")]
        public IExpression Term_Factor(IExpression factorValue)
        {
            return factorValue;
        }

        [Production("factor : primary")]
        public IExpression PrimaryFactor(IExpression primValue)
        {
            return primValue;
        }

        [Production("factor : MINUS factor")]
        public IExpression Factor(Token<ExpressionToken> minus, IExpression factorValue)
        {
            return new UnaryOperation(minus.TokenID, factorValue);
        }
    }
}