using System;
using System.Collections.Generic;
using System.Linq;
using sly.v3.lexer.regex.dfalex;
using Xunit;
using Xunit.Abstractions;

namespace ParserTests.v3.lexer.regex.dfalex
{
    public class JavaTest : TestBase
    {
        public JavaTest(ITestOutputHelper helper)
            : base(helper)
        { }

        [Fact]
        public void Test()
        {
            var builder = new DfaBuilder<JavaToken?>();
            foreach (JavaToken tok in Enum.GetValues(typeof(JavaToken)))
            {
                builder.AddPattern(tok.Pattern(), tok);
            }

            var start = builder.Build(new HashSet<JavaToken?>(Enum.GetValues(typeof(JavaToken)).Cast<JavaToken?>()), null);

            CheckDfa(start, "JavaTest.out.txt", false);
        }
    }
}