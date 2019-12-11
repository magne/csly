using sly.v3.lexer.regex.dfalex;
using Xunit;

namespace ParserTests.v3.lexer.regex.dfalex
{
    public class BitUtilsTests
    {
        [Fact]
        public void TestBitIndex()
        {
            Assert.Equal(-1, BitUtils.LowBitIndex(0));
            for (var i = 0; i < 32; ++i)
            {
                Assert.Equal(i, BitUtils.LowBitIndex(1 << i));
                Assert.Equal(i, BitUtils.LowBitIndex(5 << i));
                Assert.Equal(i, BitUtils.LowBitIndex(-1 << i));
            }
        }
    }
}