using expressionparser;
using sly.parser;
using sly.parser.generator;
using Xunit;

namespace ParserTests
{
    public class ExpressionTests
    {
        public ExpressionTests()
        {
            var parserInstance = new ExpressionParser();
            var builder = new ParserBuilder<ExpressionToken, int>();
            parser = builder.BuildParser(parserInstance, ParserType.LL_RECURSIVE_DESCENT, "expression").Result;
        }

        private readonly Parser<ExpressionToken, int> parser;

        [Fact]
        public void TestFactorDivide()
        {
            var r = parser.Parse("42/2");
            Assert.False(r.IsError);
            Assert.Equal(21, r.Result);
        }

        [Fact]
        public void TestFactorTimes()
        {
            var r = parser.Parse("2*2");
            Assert.False(r.IsError);
            Assert.Equal(4, r.Result);
        }

        [Fact]
        public void TestGroup()
        {
            var r = parser.Parse("(2 + 2)");
            Assert.False(r.IsError);
            Assert.Equal(4, r.Result);
        }

        [Fact]
        public void TestGroup2()
        {
            var r = parser.Parse("6 * (2 + 2)");
            Assert.False(r.IsError);
            Assert.Equal(24, r.Result);
        }

        [Fact]
        public void TestPrecedence()
        {
            var r = parser.Parse("6 * 2 + 2");
            Assert.False(r.IsError);
            Assert.Equal(14, r.Result);
        }

        [Fact]
        public void TestSingleNegativeValue()
        {
            var r = parser.Parse("-1");
            Assert.False(r.IsError);
            Assert.Equal(-1, r.Result);
        }


        [Fact]
        public void TestSingleValue()
        {
            var r = parser.Parse("1");
            Assert.False(r.IsError);
            Assert.Equal(1, r.Result);
        }

        [Fact]
        public void TestTermMinus()
        {
            var r = parser.Parse("1 - 1");
            Assert.False(r.IsError);
            Assert.Equal(0, r.Result);
        }

        [Fact]
        public void TestTermPlus()
        {
            var r = parser.Parse("1 + 1");
            Assert.False(r.IsError);
            Assert.Equal(2, r.Result);
        }
    }
}