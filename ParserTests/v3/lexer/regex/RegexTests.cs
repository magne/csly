using sly.lexer.regex;
using Xunit;
using Xunit.Abstractions;

namespace ParserTests.lexer.regex
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
        public void Test1()
        {
            Regex a = new Sym("A");
            Regex b = new Sym("B");
            Regex c = new Sym("C");
            Regex abStar = new Star(new Alt(a, b));
            Regex bb = new Seq(b, b);

            // The regular expression (a|b)*ab
            Regex r = new Seq(abStar, new Seq(a, b));
            BuildAndShow("dfa1.dot", r);

            // The regular expression ((a|b)*ab)*
            BuildAndShow("dfa2.dot", new Star(r));

            // The regular expression ((a|b)*ab)((a|b)*ab)
            BuildAndShow("dfa3.dot", new Seq(r, r));

            // The regular expression (a|b)*abb, from ASU 1986 p 136
            BuildAndShow("dfa4.dot", new Seq(abStar, new Seq(a, bb)));

            // SML reals: sign?((digit+(\.digit+)?))([eE]sign?digit+)?
            Regex d = new Sym("digit");
            Regex dPlus = new Seq(d, new Star(d));
            Regex s = new Sym("sign");
            Regex sOpt = new Alt(s, new Eps());
            Regex dot = new Sym(".");
            Regex dotDigOpt = new Alt(new Eps(), new Seq(dot, dPlus));
            Regex mant = new Seq(sOpt, new Seq(dPlus, dotDigOpt));
            Regex e = new Sym("e");
            Regex exp = new Alt(new Eps(), new Seq(e, new Seq(sOpt, dPlus)));
            Regex smlReal = new Seq(mant, exp);
            BuildAndShow("dfa5.dot", smlReal);
        }

        private void BuildAndShow(string filename, Regex r)
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