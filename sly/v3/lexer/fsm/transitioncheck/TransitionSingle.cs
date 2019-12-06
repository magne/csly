using System.Diagnostics.CodeAnalysis;

namespace sly.v3.lexer.fsm.transitioncheck
{
    internal class TransitionSingle : AbstractTransitionCheck
    {
        private readonly char transitionToken;

        public TransitionSingle(char token, TransitionPrecondition precondition = null)
        {
            transitionToken = token;
            Precondition = precondition;
        }

        public override bool Match(char input)
        {
            return input.Equals(transitionToken);
        }

        [ExcludeFromCodeCoverage]
        public override string ToGraphViz()
        {
            var t = "";
            if (Precondition != null) t = "[|] ";
            t += transitionToken.ToEscaped();
            return $@"[ label=""{t}"" ]";
        }
    }
}