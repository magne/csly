using System.Linq;
using sly.v3.lexer.regex;
using sly.v3.lexer.regex.dfalex;
using Xunit;
using Xunit.Abstractions;

namespace ParserTests.v3.lexer.regex
{
    // Trying the RE->NFA->DFA translation on three regular expressions
    public class RegexTests
    {
        private readonly ITestOutputHelper helper;

        public RegexTests(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        [Fact]
        public void TestManualRange()
        {
            var r = RegEx.Parse("if|((f|g|i)|(f|g|i)*)");
            helper.WriteLine(r.ToString());
            BuildAndShow("dfa1.dot", r);
        }

        [Fact]
        public void TestAutoRange()
        {
            var bld = new DfaBuilder<int>();
            bld.AddPattern(Pattern.Regex("if"), 1);
            bld.AddPattern(Pattern.Regex("[e-j][e-j]"), 2);
            var dfa = bld.Build(accepts => accepts.First());
            var a1 = dfa.GetNextState('i').GetNextState('f').GetMatch();
            Assert.Equal(1, a1);
            // var a2 = dfa.GetNextState('i').GetNextState('k').GetMatch();

            var r = RegEx.Parse("if|[e-j][e-j]*");
            helper.WriteLine(r.ToString());
            BuildAndShow("dfa1.dot", r);
        }

        [Fact]
        public void Test1()
        {
            // The regular expression (a|b)*ab
            var r = RegEx.Parse("(a|b)*ab");
            helper.WriteLine(r.ToString());
            BuildAndShow("dfa1.dot", r);

            // The regular expression ((a|b)*ab)*
            var r1 = RegEx.Parse("((a|b)*ab)*");
            helper.WriteLine(r1.ToString());
            BuildAndShow("dfa2.dot", r1);

            // The regular expression ((a|b)*ab)((a|b)*ab)
            var r2 = RegEx.Parse("((a|b)*ab)((a|b)*ab)");
            helper.WriteLine(r2.ToString());
            BuildAndShow("dfa3.dot", r2);

            var r4 = RegEx.Parse("(a|b)*ab(a|b)*ab");
            helper.WriteLine(r4.ToString());
            BuildAndShow("dfa5.dot", r4);

            // The regular expression (a|b)*abb, from ASU 1986 p 136
            var r3 = RegEx.Parse("(a|b)*abb");
            helper.WriteLine(r3.ToString());
            BuildAndShow("dfa4.dot", r3);

            // SML reals: sign?((digit+(\.digit+)?))([eE]sign?digit+)?
            RegEx d = new Sym("digit");
            RegEx dPlus = new Seq(d, new Star(d));
            RegEx s = new Sym("sign");
            RegEx sOpt = new Alt(s, Eps.Instance);
            RegEx dot = new Sym(".");
            RegEx dotDigOpt = new Alt(Eps.Instance, new Seq(dot, dPlus));
            RegEx mant = new Seq(sOpt, new Seq(dPlus, dotDigOpt));
            RegEx e = new Sym("e");
            RegEx exp = new Alt(Eps.Instance, new Seq(e, new Seq(sOpt, dPlus)));
            RegEx smlReal = new Seq(mant, exp);
            BuildAndShow("dfa5.dot", smlReal);
        }

        private void BuildAndShow(string filename, RegEx r)
        {
            Nfa nfa = r.MkNfa(new Nfa.NameSource());
            helper.WriteLine($"{nfa}\n");
            Dfa dfa = nfa.ToDfa();
            helper.WriteLine($"{dfa}\n");
            helper.WriteLine($"Writing DFA graph to file {filename}");
            var dot = dfa.WriteDot(filename);
            helper.WriteLine($"{dot}\n");
        }
    }
}