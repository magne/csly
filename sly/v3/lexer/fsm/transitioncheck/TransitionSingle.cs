using System.Diagnostics.CodeAnalysis;

namespace sly.v3.lexer.fsm.transitioncheck
{
    internal class TransitionSingle : AbstractTransitionCheck
    {
        private readonly char TransitionToken;

        public TransitionSingle(char token)
        {
            TransitionToken = token;
        }


        public TransitionSingle(char token, TransitionPrecondition precondition)
        {
            TransitionToken = token;
            Precondition = precondition;
        }

        [ExcludeFromCodeCoverage]
        public override string ToGraphViz()
        {
            var t = "";
            if (Precondition != null) t = "[|] ";
             t += TransitionToken.ToEscaped();
            return $@"[ label=""{t}"" ]";
        }

        public override bool Match(char input)
        {
            return input.Equals(TransitionToken);
        }
    }
}