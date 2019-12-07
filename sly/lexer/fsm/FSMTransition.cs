using sly.lexer.fsm.transitioncheck;
using System.Collections.Generic;
using System;

namespace sly.lexer.fsm
{
    // ReSharper disable once InconsistentNaming
    public class FSMTransition
    {
        public int FromNode;

        public int ToNode;

        internal FSMTransition(AbstractTransitionCheck check, int from, int to)
        {
            Check = check;
            FromNode = from;
            ToNode = to;
        }

        public AbstractTransitionCheck Check { get; set; }


        public string ToGraphViz<TNode>(Dictionary<int, FSMNode<TNode>> nodes)
        {
            string f = "\""+(nodes[FromNode].Mark ?? "")+ " #"+FromNode+"\"";
            string t = "\""+(nodes[ToNode].Mark ?? "")+ " #"+ToNode+"\"";
            return $"{f} -> {t} {Check.ToGraphViz()}";
        }


        internal bool Match(char token, ReadOnlyMemory<char> value)
        {
            return Check.Check(token, value);
        }

        internal bool Match(char token)
        {
            return Check.Match(token);
        }
    }
}