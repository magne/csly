﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace sly.v3.lexer.fsm.transitioncheck
{
    internal class TransitionAnyExcept : AbstractTransitionCheck
    {
        private readonly List<char> TokenExceptions;

        public TransitionAnyExcept(params char[] tokens)
        {
            TokenExceptions = new List<char>();
            TokenExceptions.AddRange(tokens);
        }

        public TransitionAnyExcept(TransitionPrecondition precondition, params char[] tokens)
        {
            TokenExceptions = new List<char>();
            TokenExceptions.AddRange(tokens);
            Precondition = precondition;
        }

        [ExcludeFromCodeCoverage]
        public override string ToGraphViz()
        {
           var label = "";
            if (Precondition != null) label = "[|] ";
            label += $"^({string.Join(", ",TokenExceptions.Select(c => c.ToEscaped()))})";
            return $@"[ label=""{label}"" ]";
        }

        public override bool Match(char input)
        {
            return !TokenExceptions.Contains(input);
        }
    }
}