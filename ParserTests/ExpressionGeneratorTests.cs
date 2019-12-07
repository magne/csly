using System.Collections.Generic;
using expressionparser;
using simpleExpressionParser;
using sly.buildresult;
using sly.parser;
using sly.parser.generator;
using Xunit;

namespace ParserTests
{
    public class ExpressionGeneratorTests
    {
        private BuildResult<Parser<ExpressionToken, int>> parser;

        private string startingRule = "";


        private void BuildParser()
        {
            startingRule = $"{typeof(SimpleExpressionParser).Name}_expressions";
            var parserInstance = new SimpleExpressionParser();
            var builder = new ParserBuilder<ExpressionToken, int>();
            parser = builder.BuildParser(parserInstance, ParserType.LL_RECURSIVE_DESCENT, startingRule);
        }

        [Fact]
        public void TestAssociativityFactor()
        {
            BuildParser();
            var r = parser.Result.Parse("1 / 2 / 3", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(1 / 2 / 3, r.Result);


            r = parser.Result.Parse("1 / 2 / 3 / 4", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(1 / 2 / 3 / 4, r.Result);


            r = parser.Result.Parse("1 / 2 * 3", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(1 / 2 * 3, r.Result);
        }

        [Fact]
        public void TestAssociativityTerm()
        {
            BuildParser();
            var r = parser.Result.Parse("1 - 2 - 3", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(1 - 2 - 3, r.Result);


            r = parser.Result.Parse("1 - 2 - 3 - 4", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(1 - 2 - 3 - 4, r.Result);


            r = parser.Result.Parse("1 - 2 + 3", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(1 - 2 + 3, r.Result);
        }

        [Fact]
        public void TestBuild()
        {
            BuildParser();
            Assert.False(parser.IsError);
            Assert.Equal(6, parser.Result.Configuration.NonTerminals.Count);
            var nonterminals = new List<NonTerminal<ExpressionToken>>();
            foreach (var pair in parser.Result.Configuration.NonTerminals) nonterminals.Add(pair.Value);
            var nt = nonterminals[0]; // operan
            Assert.Single(nt.Rules);
            Assert.Equal("operand", nt.Name);
            nt = nonterminals[1];
            Assert.Equal(2, nt.Rules.Count);
            Assert.Contains("primary_value", nt.Name);
            nt = nonterminals[2];
            Assert.Equal(3, nt.Rules.Count);
            Assert.Contains("10", nt.Name);
            Assert.Contains("PLUS", nt.Name);
            Assert.Contains("MINUS", nt.Name);
            nt = nonterminals[3];
            Assert.Equal(3, nt.Rules.Count);
            Assert.Contains("50", nt.Name);
            Assert.Contains("TIMES", nt.Name);
            Assert.Contains("DIVIDE", nt.Name);
            nt = nonterminals[4];
            Assert.Equal(3, nt.Rules.Count);
            Assert.Contains("100", nt.Name);
            Assert.Contains("MINUS", nt.Name);
            nt = nonterminals[5];
            Assert.Single(nt.Rules);
            Assert.Equal(startingRule, nt.Name);
            Assert.Single(nt.Rules[0].Clauses);
        }

        [Fact]
        public void TestFactorDivide()
        {
            BuildParser();
            var r = parser.Result.Parse("42/2", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(21, r.Result);
        }

        [Fact]
        public void TestFactorTimes()
        {
            BuildParser();
            var r = parser.Result.Parse("2*2", startingRule);
            Assert.False(r.IsError);
            Assert.IsType<int>(r.Result);
            Assert.Equal(4, r.Result);
        }

        [Fact]
        public void TestGroup()
        {
            BuildParser();
            var r = parser.Result.Parse("(-1 + 2)  * 3", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(3, r.Result);
        }

        [Fact]
        public void TestPostFix()
        {
            BuildParser();
            var r = parser.Result.Parse("10!", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(3628800, r.Result);
        }


        [Fact]
        public void TestPrecedence()
        {
            BuildParser();
            var r = parser.Result.Parse("-1 + 2  * 3", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(5, r.Result);
        }

        [Fact]
        public void TestSingleNegativeValue()
        {
            BuildParser();
            var r = parser.Result.Parse("-1", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(-1, r.Result);
        }


        [Fact]
        public void TestSingleValue()
        {
            BuildParser();
            var r = parser.Result.Parse("1", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(1, r.Result);
        }

        [Fact]
        public void TestTermMinus()
        {
            BuildParser();
            var r = parser.Result.Parse("1 - 1", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(0, r.Result);
        }

        [Fact]
        public void TestTermPlus()
        {
            BuildParser();
            var r = parser.Result.Parse("1 + 1", startingRule);
            Assert.False(r.IsError);
            Assert.IsType<int>(r.Result);
            Assert.Equal(2, r.Result);
        }

        [Fact]
        public void TestUnaryPrecedence()
        {
            BuildParser();
            var r = parser.Result.Parse("-1 * 2", startingRule);
            Assert.False(r.IsError);
            Assert.Equal(-2, r.Result);
        }
    }
}