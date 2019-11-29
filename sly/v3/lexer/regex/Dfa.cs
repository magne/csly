using System.Collections.Generic;
using System.Text;

namespace sly.lexer.regex
{
    // Class Dfa, deterministic finite automata

    /*
     * A deterministic finite automaton (DFA) is represented as a Map from state number (int) to a Map from label (a string, non-null) to a target
     * state (an int).
     */
    internal class Dfa
    {
        private readonly int startState;
        private readonly ISet<int> acceptStates;
        private readonly IDictionary<int, IDictionary<string, int>> trans;

        public Dfa(int startState, ISet<int> acceptStates, IDictionary<int, IDictionary<string, int>> trans)
        {
            this.startState = startState;
            this.acceptStates = acceptStates;
            this.trans = trans;
        }

        public int Start => startState;

        public ISet<int> Accept => acceptStates;

        public IDictionary<int, IDictionary<string, int>> Trans => trans;

        public override string ToString()
        {
            return $"DFA start={startState}\naccept={{ {string.Join(", ", acceptStates)} }}";
        }

        // Write an input file for the dot program.  You can find dot at
        // http://www.research.att.com/sw/tools/graphviz/
        public string WriteDot(string filename)
        {
            var buf = new StringBuilder();
            buf.Append("// Format this file as a Postscript file with\n");
            buf.Append($"//    dot {filename} -Tps -o out.ps\n");
            buf.Append("digraph dfa {\n");
            // buf.Append("size=\"11,8.25\";\n");
            // buf.Append("rotate=90;\n");
            buf.Append("rankdir=LR;\n");
            buf.Append("n999999 [style=invis];\n"); // Invisible start node
            buf.Append($"n999999 -> n{startState}\n"); // Edge into start state

            // Accept states are double circles
            foreach (var state in trans.Keys)
            {
                if (acceptStates.Contains(state))
                {
                    buf.Append($"n{state}[peripheries=2];\n");
                }
            }

            // The transitions
            foreach (var entry in trans)
            {
                var s1 = entry.Key;
                foreach (var s1Trans in entry.Value)
                {
                    var lab = s1Trans.Key;
                    var s2 = s1Trans.Value;
                    buf.Append($"n{s1} -> n{s2} [label=\"{lab}\"];\n");
                }
            }

            buf.Append("}\n");
            return buf.ToString();
        }
    }
}