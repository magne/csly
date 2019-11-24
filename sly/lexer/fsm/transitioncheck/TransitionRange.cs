using System.Diagnostics.CodeAnalysis;

namespace sly.lexer.fsm.transitioncheck
{
    public class TransitionRange : AbstractTransitionCheck
    {
        private readonly char rangeStart;
        private readonly char rangeEnd;

        public TransitionRange(char start, char end, TransitionPrecondition precondition = null)
        {
            rangeStart = start;
            rangeEnd = end;
            Precondition = precondition;
        }

        public override bool Match(char input)
        {
            return input.CompareTo(rangeStart) >= 0 && input.CompareTo(rangeEnd) <= 0;
        }

        [ExcludeFromCodeCoverage]
        public override string ToGraphViz()
        {
            var t = "";
            if (Precondition != null) t = "[|] ";
            t += $"[{rangeStart.ToEscaped()}-{rangeEnd.ToEscaped()}]";
            return $@"[ label=""{t}"" ]";
        }
    }
}