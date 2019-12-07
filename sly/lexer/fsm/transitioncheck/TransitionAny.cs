using System.Diagnostics.CodeAnalysis;

namespace sly.lexer.fsm.transitioncheck
{
    public class TransitionAny : AbstractTransitionCheck
    {
        // ReSharper disable once UnusedParameter.Local
        public TransitionAny(char token)
        {
        }

        // ReSharper disable once UnusedParameter.Local
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