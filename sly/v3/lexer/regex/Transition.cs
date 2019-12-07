namespace sly.v3.lexer.regex
{
    // Class Transition, a transition from one state to another
    internal class Transition
    {
        public readonly string Lab;
        public readonly int Target;

        public Transition(string lab, int target)
        {
            this.Lab = lab;
            this.Target = target;
        }

        public override string ToString()
        {
            return $"-{Lab}-> {Target}";
        }
    }
}