using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace sly.lexer.fsm.transitioncheck
{
    public class TransitionAnyExcept : AbstractTransitionCheck
    {
        private readonly List<char> tokenExceptions;

        public TransitionAnyExcept(char[] tokens, TransitionPrecondition precondition = null)
        {
            tokenExceptions = new List<char>();
            tokenExceptions.AddRange(tokens);
            Precondition = precondition;
        }

        public override bool Match(char input)
        {
            return !tokenExceptions.Contains(input);
        }

        [ExcludeFromCodeCoverage]
        public override string ToGraphViz()
        {
            var label = "";
            if (Precondition != null) label = "[|] ";
            label += $"^({string.Join(", ", tokenExceptions.Select(c => c.ToEscaped()))})";
            return $@"[ label=""{label}"" ]";
        }
    }
}