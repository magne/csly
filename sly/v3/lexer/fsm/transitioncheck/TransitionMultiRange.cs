using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace sly.v3.lexer.fsm.transitioncheck
{
    internal class TransitionMultiRange : AbstractTransitionCheck
    {
        private readonly (char start, char end)[] ranges;

        public TransitionMultiRange(params (char start, char end)[] ranges)
        {
            this.ranges = ranges;
        }

        public TransitionMultiRange(TransitionPrecondition precondition, params (char start, char end)[] ranges) : this(ranges)
        {
            Precondition = precondition;
        }

        public override bool Match(char input)
        {
            var match = false;
            for (var i = 0; !match && i < ranges.Length; i++)
            {
                var (start, end) = ranges[i];
                match = input.CompareTo(start) >= 0 && input.CompareTo(end) <= 0;
            }

            return match;
        }

        [ExcludeFromCodeCoverage]
        public override string ToGraphViz()
        {
            var builder = new StringBuilder();

            if (Precondition != null)
            {
                builder.Append("[|] ");
            }

            builder.Append("[");
            foreach (var (start, end) in ranges)
            {
                builder
                    .Append(start)
                    .Append("-")
                    .Append(end)
                    .Append(",");
            }

            builder.Append("]");

            return $@"[ label=""{builder}"" ]";
        }
    }
}