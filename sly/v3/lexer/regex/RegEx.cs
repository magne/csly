using System;

namespace sly.v3.lexer.regex
{
    // Regular expressions
    //
    // Abstract syntax of regular expressions
    //    r ::= A | r1 r2 | (r1|r2) | r*
    internal abstract class RegEx
    {
        public abstract Nfa MkNfa(Nfa.NameSource names);

        protected internal abstract int Priority { get; }

        public static RegEx Parse(string s)
        {
            return new RexExParser(s).regex();
        }


        private class RexExParser
        {
            private readonly string input;

            private int index;

            public RexExParser(string input)
            {
                this.input = input;
            }

            private char Peek()
            {
                return input[index];
            }

            private void Eat(char ch)
            {
                if (Peek() == ch)
                {
                    index++;
                }
                else
                {
                    throw new Exception($"Expected: {ch}; got: {Peek()}.");
                }
            }

            private char Next()
            {
                var ch = Peek();
                Eat(ch);
                return ch;
            }

            private bool More()
            {
                return index < input.Length;
            }

            internal RegEx regex()
            {
                var term = this.term();

                if (More() && Peek() == '|')
                {
                    Eat('|');
                    var regex = this.regex();
                    return new Alt(term, regex);
                }

                return term;
            }

            private RegEx term()
            {
                RegEx factor = Eps.Instance;

                while (More() && Peek() != ')' && Peek() != '|')
                {
                    var nextFactor = this.factor();
                    factor = new Seq(factor, nextFactor);
                }

                return factor;
            }

            private RegEx factor()
            {
                var @base = this.@base();

                while (More() && Peek() == '*')
                {
                    Eat('*');
                    @base = new Star(@base);
                }

                return @base;
            }

            private RegEx @base()
            {
                switch (Peek())
                {
                    case '(':
                        Eat('(');
                        var r = regex();
                        Eat(')');
                        return r;

                    case '\\':
                        Eat('\\');
                        var esc = Next();
                        return new Sym(esc.ToString());

                    default:
                        return new Sym(Next().ToString());
                }
            }
        }
    }

    internal class Eps : RegEx
    {
        public static readonly Eps Instance = new Eps();

        private Eps()
        { }

        // The resulting nfa0 has form s0s -eps-> s0e
        public override Nfa MkNfa(Nfa.NameSource names)
        {
            var s0s = Nfa.NameSource.Next();
            var s0e = Nfa.NameSource.Next();
            var nfa0 = new Nfa(s0s, s0e);
            nfa0.AddTrans(s0s, null, s0e);
            return nfa0;
        }

        protected internal override int Priority => 0;

        public override bool Equals(object obj)
        {
            return this == obj;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return string.Empty;
        }
    }


    internal class Sym : RegEx
    {
        private readonly string sym;

        public Sym(string sym)
        {
            this.sym = sym;
        }

        // The resulting nfa0 has form s0s -sym-> s0e
        public override Nfa MkNfa(Nfa.NameSource names)
        {
            var s0s = Nfa.NameSource.Next();
            var s0e = Nfa.NameSource.Next();
            var nfa0 = new Nfa(s0s, s0e);
            nfa0.AddTrans(s0s, sym, s0e);
            return nfa0;
        }

        protected internal override int Priority => 0;

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is Sym other)
            {
                return sym.Equals(other.sym);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return sym?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return sym;
        }
    }

    internal class Seq : RegEx
    {
        private readonly RegEx r1;
        private readonly RegEx r2;

        public Seq(RegEx r1, RegEx r2)
        {
            this.r1 = r1;
            this.r2 = r2;
        }

        // If   nfa1 has form s1s ----> s1e
        // and  nfa2 has form s2s ----> s2e
        // then nfa0 has form s1s ----> s1e -eps-> s2s ----> s2e
        public override Nfa MkNfa(Nfa.NameSource names)
        {
            var nfa1 = r1.MkNfa(names);
            var nfa2 = r2.MkNfa(names);
            var nfa0 = new Nfa(nfa1.Start, nfa2.Exit);
            foreach (var entry in nfa1.Trans)
            {
                nfa0.AddTrans(entry);
            }

            foreach (var entry in nfa2.Trans)
            {
                nfa0.AddTrans(entry);
            }

            nfa0.AddTrans(nfa1.Exit, null, nfa2.Start);
            return nfa0;
        }

        protected internal override int Priority => 2;

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is Seq other)
            {
                return r1.Equals(other.r1) && r2.Equals(other.r2);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return r1.GetHashCode() * 31 + r2.GetHashCode();
        }

        public override string ToString()
        {
            if (Priority < r1.Priority || Priority < r2.Priority)
            {
                return $"({r1}{r2})";
            }

            return $"{r1}{r2}";
        }
    }

    internal class Alt : RegEx
    {
        private readonly RegEx r1;
        private readonly RegEx r2;

        public Alt(RegEx r1, RegEx r2)
        {
            this.r1 = r1;
            this.r2 = r2;
        }

        // If   nfa1 has form s1s ----> s1e
        // and  nfa2 has form s2s ----> s2e
        // then nfa0 has form s0s -eps-> s1s ----> s1e -eps-> s0e
        //                    s0s -eps-> s2s ----> s2e -eps-> s0e
        public override Nfa MkNfa(Nfa.NameSource names)
        {
            var nfa1 = r1.MkNfa(names);
            var nfa2 = r2.MkNfa(names);
            var s0s = Nfa.NameSource.Next();
            var s0e = Nfa.NameSource.Next();
            var nfa0 = new Nfa(s0s, s0e);
            foreach (var entry in nfa1.Trans)
            {
                nfa0.AddTrans(entry);
            }

            foreach (var entry in nfa2.Trans)
            {
                nfa0.AddTrans(entry);
            }

            nfa0.AddTrans(s0s,       null, nfa1.Start);
            nfa0.AddTrans(s0s,       null, nfa2.Start);
            nfa0.AddTrans(nfa1.Exit, null, s0e);
            nfa0.AddTrans(nfa2.Exit, null, s0e);
            return nfa0;
        }

        protected internal override int Priority => 3;

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is Alt other)
            {
                return r1.Equals(other.r1) && r2.Equals(other.r2);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return r1.GetHashCode() * 31 + r2.GetHashCode();
        }

        public override string ToString()
        {
            if (Priority < r1.Priority || Priority < r2.Priority)
            {
                return $"({r1}|{r2})";
            }

            return $"{r1}|{r2}";
        }
    }

    internal class Star : RegEx
    {
        private readonly RegEx r;

        public Star(RegEx r)
        {
            this.r = r;
        }

        // If   nfa1 has form s1s ----> s1e
        // then nfa0 has form s0s ----> s0s
        //                    s0s -eps-> s1s
        //                    s1e -eps-> s0s
        public override Nfa MkNfa(Nfa.NameSource names)
        {
            var nfa1 = r.MkNfa(names);
            var s0s = Nfa.NameSource.Next();
            var nfa0 = new Nfa(s0s, s0s);
            foreach (var entry in nfa1.Trans)
            {
                nfa0.AddTrans(entry);
            }

            nfa0.AddTrans(s0s,       null, nfa1.Start);
            nfa0.AddTrans(nfa1.Exit, null, s0s);
            return nfa0;
        }

        protected internal override int Priority => 1;

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is Star other)
            {
                return r.Equals(other.r);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return r.GetHashCode();
        }

        public override string ToString()
        {
            if (Priority < r.Priority)
            {
                return $"({r})*";
            }

            return $"{r}*";
        }
    }
}