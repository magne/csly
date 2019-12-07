using System.Diagnostics.CodeAnalysis;

namespace sly.lexer.fsm.transitioncheck
{
    public class TransitionRange : AbstractTransitionCheck
    {
        private readonly char rangeEnd;
        private readonly char rangeStart;

        public TransitionRange(char start, char end)
        {
            rangeStart = start;
            rangeEnd = end;
        }


        public TransitionRange(char start, char end, TransitionPrecondition precondition)
        {
            rangeStart = start;
            rangeEnd = end;
            Precondition = precondition;
        }

        [ExcludeFromCodeCoverage]
        public override string ToGraphViz()
        {
            var t = "";
            if (Precondition != null) t = "[|] ";
            t += $"[{rangeStart.ToEscaped()}-{rangeEnd.ToEscaped()}]";
            return $@"[ label=""{t}"" ]";
        }


        public override bool Match(char input)
        {
            return input.CompareTo(rangeStart) >= 0 && input.CompareTo(rangeEnd) <= 0;
        }
    }
}