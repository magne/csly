﻿using sly.lexer.fsm.transitioncheck;
using System.Collections.Generic;
using System;

namespace sly.lexer.fsm
{
    public class FSMTransition
    {
        private readonly int fromNode;

        public readonly int ToNode;

        internal FSMTransition(AbstractTransitionCheck check, int from, int to)
        {
            Check = check;
            fromNode = from;
            ToNode = to;
        }

        private AbstractTransitionCheck Check { get; set; }


        public string ToGraphViz<N>(Dictionary<int, FSMNode<N>> nodes)
        {
            return $"{nodes[fromNode]} -> {nodes[ToNode]} {Check.ToGraphViz()}";
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