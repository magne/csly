namespace sly.v3.lexer.regex
{
    // Class Transition, a transition from one state to another
    internal class Transition
    {
        public readonly string lab;
        public readonly int target;

        public Transition(string lab, int target)
        {
            this.lab = lab;
            this.target = target;
        }

        public override string ToString()
        {
            return $"-{lab}-> {target}";
        }
    }
}