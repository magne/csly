using System.Collections.Generic;
using System.Linq;
using csly.whileLang.model;
using sly.lexer;
using sly.parser.generator;

namespace csly.whileLang.parser
{
    public class WhileParser
    {
        #region COMPARISON OPERATIONS

        [Operation((int) WhileToken.LESSER, Affix.InFix, Associativity.Right, 50)]
        [Operation((int) WhileToken.GREATER, Affix.InFix, Associativity.Right, 50)]
        [Operation((int) WhileToken.EQUALS, Affix.InFix, Associativity.Right, 50)]
        [Operation((int) WhileToken.DIFFERENT, Affix.InFix, Associativity.Right, 50)]
        public IWhileAst BinaryComparisonExpression(IWhileAst left, Token<WhileToken> operatorToken, IWhileAst right)
        {
            var oper = BinaryOperator.ADD;

            switch (operatorToken.TokenID)
            {
                case WhileToken.LESSER:
                {
                    oper = BinaryOperator.LESSER;
                    break;
                }
                case WhileToken.GREATER:
                {
                    oper = BinaryOperator.GREATER;
                    break;
                }
                case WhileToken.EQUALS:
                {
                    oper = BinaryOperator.EQUALS;
                    break;
                }
                case WhileToken.DIFFERENT:
                {
                    oper = BinaryOperator.DIFFERENT;
                    break;
                }
            }

            var operation = new BinaryOperation(left as IExpression, oper, right as IExpression);
            return operation;
        }

        #endregion

        #region STRING OPERATIONS

        [Operation((int) WhileToken.CONCAT, Affix.InFix, Associativity.Right, 10)]
        public IWhileAst BinaryStringExpression(IWhileAst left, Token<WhileToken> operatorToken, IWhileAst right)
        {
            var oper = BinaryOperator.CONCAT;
            var operation = new BinaryOperation(left as IExpression, oper, right as IExpression);
            return operation;
        }

        #endregion

        #region statements

        [Production("statement :  LPAREN [d] statement RPAREN [d]")]
        public IWhileAst Block(IStatement statement)
        {
            return statement;
        }

        [Production("statement : sequence")]
        public IWhileAst StatementSequence(IWhileAst sequence)
        {
            return sequence;
        }

        [Production("sequence : statementPrim additionalStatements*")]
        public IWhileAst SequenceStatements(IWhileAst first, List<IWhileAst> next)
        {
            var seq = new SequenceStatement(first as IStatement);
            seq.AddRange(next.Cast<IStatement>().ToList());
            return seq;
        }

        [Production("additionalStatements : SEMICOLON [d] statementPrim")]
        public IWhileAst Additional(IWhileAst statement)
        {
            return statement;
        }

        [Production("statementPrim: IF [d] WhileParser_expressions THEN [d] statement ELSE [d] statement")]
        public IWhileAst IfStmt(IWhileAst cond, IWhileAst thenStmt, IStatement elseStmt)
        {
            var stmt = new IfStatement(cond as IExpression, thenStmt as IStatement, elseStmt);
            return stmt;
        }

        [Production("statementPrim: WHILE [d] WhileParser_expressions DO [d] statement")]
        public IWhileAst WhileStmt(IWhileAst cond, IWhileAst blockStmt)
        {
            var stmt = new WhileStatement(cond as IExpression, blockStmt as IStatement);
            return stmt;
        }

        [Production("statementPrim: IDENTIFIER ASSIGN [d] WhileParser_expressions")]
        public IWhileAst AssignStmt(Token<WhileToken> variable, IExpression value)
        {
            var assign = new AssignStatement(variable.StringWithoutQuotes, value);
            return assign;
        }

        [Production("statementPrim: SKIP [d]")]
        public IWhileAst SkipStmt()
        {
            return new SkipStatement();
        }

        [Production("statementPrim: RETURN [d] WhileParser_expressions")]
        public IWhileAst ReturnStmt(IWhileAst expression)
        {
            return new ReturnStatement(expression as IExpression);
        }

        [Production("statementPrim: PRINT [d] WhileParser_expressions")]
        public IWhileAst SkipStmt(IWhileAst expression)
        {
            return new PrintStatement(expression as IExpression);
        }

        #endregion


        #region OPERANDS

        [Production("primary: INT")]
        public IWhileAst PrimaryInt(Token<WhileToken> intToken)
        {
            return new IntegerConstant(intToken.IntValue);
        }

        [Production("primary: TRUE")]
        [Production("primary: FALSE")]
        public IWhileAst PrimaryBool(Token<WhileToken> boolToken)
        {
            return new BoolConstant(bool.Parse(boolToken.StringWithoutQuotes));
        }

        [Production("primary: STRING")]
        public IWhileAst PrimaryString(Token<WhileToken> stringToken)
        {
            return new StringConstant(stringToken.StringWithoutQuotes);
        }

        [Production("primary: IDENTIFIER")]
        public IWhileAst PrimaryId(Token<WhileToken> varToken)
        {
            return new Variable(varToken.StringWithoutQuotes);
        }

        [Operand]
        [Production("operand: primary")]
        public IWhileAst Operand(IWhileAst prim)
        {
            return prim;
        }

        #endregion

        #region NUMERIC OPERATIONS

        [Operation((int) WhileToken.PLUS, Affix.InFix, Associativity.Right, 10)]
        [Operation((int) WhileToken.MINUS, Affix.InFix, Associativity.Right, 10)]
        public IWhileAst BinaryTermNumericExpression(IWhileAst left, Token<WhileToken> operatorToken, IWhileAst right)
        {
            var oper = BinaryOperator.ADD;

            switch (operatorToken.TokenID)
            {
                case WhileToken.PLUS:
                {
                    oper = BinaryOperator.ADD;
                    break;
                }
                case WhileToken.MINUS:
                {
                    oper = BinaryOperator.SUB;
                    break;
                }
            }

            var operation = new BinaryOperation(left as IExpression, oper, right as IExpression);
            return operation;
        }

        [Operation((int) WhileToken.TIMES, Affix.InFix, Associativity.Right, 50)]
        [Operation((int) WhileToken.DIVIDE, Affix.InFix, Associativity.Right, 50)]
        public IWhileAst BinaryFactorNumericExpression(IWhileAst left, Token<WhileToken> operatorToken, IWhileAst right)
        {
            var oper = BinaryOperator.MULTIPLY;

            switch (operatorToken.TokenID)
            {
                case WhileToken.TIMES:
                {
                    oper = BinaryOperator.MULTIPLY;
                    break;
                }
                case WhileToken.DIVIDE:
                {
                    oper = BinaryOperator.DIVIDE;
                    break;
                }
            }

            var operation = new BinaryOperation(left as IExpression, oper, right as IExpression);
            return operation;
        }

        [Operation((int) WhileToken.MINUS, Affix.PreFix, Associativity.Right, 100)]
        public IWhileAst UnaryNumericExpression(Token<WhileToken> operation, IWhileAst value)
        {
            return new Neg(value as IExpression);
        }

        #endregion


        #region BOOLEAN OPERATIONS

        [Operation((int) WhileToken.OR, Affix.InFix, Associativity.Right, 10)]
        public IWhileAst BinaryOrExpression(IWhileAst left, Token<WhileToken> operatorToken, IWhileAst right)
        {
            var oper = BinaryOperator.OR;


            var operation = new BinaryOperation(left as IExpression, oper, right as IExpression);
            return operation;
        }

        [Operation((int) WhileToken.AND, Affix.InFix, Associativity.Right, 50)]
        public IWhileAst BinaryAndExpression(IWhileAst left, Token<WhileToken> operatorToken, IWhileAst right)
        {
            var oper = BinaryOperator.AND;


            var operation = new BinaryOperation(left as IExpression, oper, right as IExpression);
            return operation;
        }

        [Operation((int) WhileToken.NOT, Affix.PreFix, Associativity.Right, 100)]
        public IWhileAst BinaryOrExpression(Token<WhileToken> operatorToken, IWhileAst value)
        {
            return new Not(value as IExpression);
        }

        #endregion
    }
}