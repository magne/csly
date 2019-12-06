﻿using System.Diagnostics.CodeAnalysis;

namespace sly.v3.lexer.fsm.transitioncheck
{
    internal class TransitionAny : AbstractTransitionCheck
    {
        public TransitionAny(char token)
        {
        }

        public TransitionAny(char token, TransitionPrecondition transitionPrecondition)
        {
            Precondition = transitionPrecondition;
        }

        [ExcludeFromCodeCoverage]
        public override string ToGraphViz()
        {
            var label = (Precondition != null) ? "[|]*" : "*";
            return  $@"[ label=""{label}"" ]";
        }

        public override bool Match(char input)
        {
            return true;
        }
    }
}