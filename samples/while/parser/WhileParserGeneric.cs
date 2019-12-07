using System.Collections.Generic;
using System.Linq;
using csly.whileLang.model;
using sly.lexer;
using sly.parser.generator;

namespace csly.whileLang.parser
{
    public class WhileParserGeneric
    {
        #region COMPARISON OPERATIONS

        [Operation((int) WhileTokenGeneric.LESSER, Affix.InFix, Associativity.Right, 50)]
        [Operation((int) WhileTokenGeneric.GREATER, Affix.InFix, Associativity.Right, 50)]
        [Operation((int) WhileTokenGeneric.EQUALS, Affix.InFix, Associativity.Right, 50)]
        [Operation((int) WhileTokenGeneric.DIFFERENT, Affix.InFix, Associativity.Right, 50)]
        public IWhileAst BinaryComparisonExpression(IWhileAst left, Token<WhileTokenGeneric> operatorToken,
            IWhileAst right)
        {
            var oper = BinaryOperator.ADD;

            switch (operatorToken.TokenID)
            {
                case WhileTokenGeneric.LESSER:
                {
                    oper = BinaryOperator.LESSER;
                    break;
                }
                case WhileTokenGeneric.GREATER:
                {
                    oper = BinaryOperator.GREATER;
                    break;
                }
                case WhileTokenGeneric.EQUALS:
                {
                    oper = BinaryOperator.EQUALS;
                    break;
                }
                case WhileTokenGeneric.DIFFERENT:
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

        [Operation((int) WhileTokenGeneric.CONCAT, Affix.InFix, Associativity.Right, 10)]
        public IWhileAst BinaryStringExpression(IWhileAst left, Token<WhileTokenGeneric> operatorToken, IWhileAst right)
        {
            var oper = BinaryOperator.CONCAT;
            var operation = new BinaryOperation(left as IExpression, oper, right as IExpression);
            return operation;
        }

        #endregion

        #region statements

        [Production("statement :  LPAREN statement RPAREN ")]
        public IWhileAst Block(Token<WhileTokenGeneric> discardLpar, IStatement statement,
            Token<WhileTokenGeneric> discardRpar)
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

        [Production("additionalStatements : SEMICOLON statementPrim")]
        public IWhileAst Additional(Token<WhileTokenGeneric> semi, IWhileAst statement)
        {
            return statement;
        }

        [Production("statementPrim: IF WhileParserGeneric_expressions THEN statement ELSE statement")]
        public IWhileAst IfStmt(Token<WhileTokenGeneric> discardIf, IWhileAst cond, Token<WhileTokenGeneric> dicardThen,
            IWhileAst thenStmt, Token<WhileTokenGeneric> dicardElse, IStatement elseStmt)
        {
            var stmt = new IfStatement(cond as IExpression, thenStmt as IStatement, elseStmt);
            return stmt;
        }

        [Production("statementPrim: WHILE WhileParserGeneric_expressions DO statement")]
        public IWhileAst WhileStmt(Token<WhileTokenGeneric> discardWhile, IWhileAst cond,
            Token<WhileTokenGeneric> dicardDo, IWhileAst blockStmt)
        {
            var stmt = new WhileStatement(cond as IExpression, blockStmt as IStatement);
            return stmt;
        }

        [Production("statementPrim: IDENTIFIER ASSIGN WhileParserGeneric_expressions")]
        public IWhileAst AssignStmt(Token<WhileTokenGeneric> variable, Token<WhileTokenGeneric> discardAssign,
            IExpression value)
        {
            var assign = new AssignStatement(variable.StringWithoutQuotes, value);
            return assign;
        }

        [Production("statementPrim: SKIP")]
        public IWhileAst SkipStmt(Token<WhileTokenGeneric> discard)
        {
            return new SkipStatement();
        }

        [Production("statementPrim: PRINT WhileParserGeneric_expressions")]
        public IWhileAst SkipStmt(Token<WhileTokenGeneric> discard, IWhileAst expression)
        {
            return new PrintStatement(expression as IExpression);
        }

        #endregion


        #region OPERANDS

        [Production("primary: INT")]
        public IWhileAst PrimaryInt(Token<WhileTokenGeneric> intToken)
        {
            return new IntegerConstant(intToken.IntValue);
        }

        [Production("primary: TRUE")]
        [Production("primary: FALSE")]
        public IWhileAst PrimaryBool(Token<WhileTokenGeneric> boolToken)
        {
            return new BoolConstant(bool.Parse(boolToken.StringWithoutQuotes));
        }

        [Production("primary: STRING")]
        public IWhileAst PrimaryString(Token<WhileTokenGeneric> stringToken)
        {
            return new StringConstant(stringToken.StringWithoutQuotes);
        }

        [Production("primary: IDENTIFIER")]
        public IWhileAst PrimaryId(Token<WhileTokenGeneric> varToken)
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

        [Operation((int) WhileTokenGeneric.PLUS, Affix.InFix, Associativity.Right, 10)]
        [Operation((int) WhileTokenGeneric.MINUS, Affix.InFix, Associativity.Right, 10)]
        public IWhileAst BinaryTermNumericExpression(IWhileAst left, Token<WhileTokenGeneric> operatorToken,
            IWhileAst right)
        {
            var oper = BinaryOperator.ADD;

            switch (operatorToken.TokenID)
            {
                case WhileTokenGeneric.PLUS:
                {
                    oper = BinaryOperator.ADD;
                    break;
                }
                case WhileTokenGeneric.MINUS:
                {
                    oper = BinaryOperator.SUB;
                    break;
                }
            }

            var operation = new BinaryOperation(left as IExpression, oper, right as IExpression);
            return operation;
        }

        [Operation((int) WhileTokenGeneric.TIMES, Affix.InFix, Associativity.Right, 50)]
        [Operation((int) WhileTokenGeneric.DIVIDE, Affix.InFix, Associativity.Right, 50)]
        public IWhileAst BinaryFactorNumericExpression(IWhileAst left, Token<WhileTokenGeneric> operatorToken,
            IWhileAst right)
        {
            var oper = BinaryOperator.MULTIPLY;

            switch (operatorToken.TokenID)
            {
                case WhileTokenGeneric.TIMES:
                {
                    oper = BinaryOperator.MULTIPLY;
                    break;
                }
                case WhileTokenGeneric.DIVIDE:
                {
                    oper = BinaryOperator.DIVIDE;
                    break;
                }
            }

            var operation = new BinaryOperation(left as IExpression, oper, right as IExpression);
            return operation;
        }

        [Operation((int) WhileTokenGeneric.MINUS, Affix.PreFix, Associativity.Right, 100)]
        public IWhileAst UnaryNumericExpression(Token<WhileTokenGeneric> operation, IWhileAst value)
        {
            return new Neg(value as IExpression);
        }

        #endregion


        #region BOOLEAN OPERATIONS

        [Operation((int) WhileTokenGeneric.OR, Affix.InFix, Associativity.Right, 10)]
        public IWhileAst BinaryOrExpression(IWhileAst left, Token<WhileTokenGeneric> operatorToken, IWhileAst right)
        {
            var oper = BinaryOperator.OR;


            var operation = new BinaryOperation(left as IExpression, oper, right as IExpression);
            return operation;
        }

        [Operation((int) WhileTokenGeneric.AND, Affix.InFix, Associativity.Right, 50)]
        public IWhileAst BinaryAndExpression(IWhileAst left, Token<WhileTokenGeneric> operatorToken, IWhileAst right)
        {
            var oper = BinaryOperator.AND;


            var operation = new BinaryOperation(left as IExpression, oper, right as IExpression);
            return operation;
        }

        [Operation((int) WhileTokenGeneric.NOT, Affix.PreFix, Associativity.Right, 100)]
        public IWhileAst BinaryOrExpression(Token<WhileTokenGeneric> operatorToken, IWhileAst value)
        {
            return new Not(value as IExpression);
        }

        #endregion
    }
}