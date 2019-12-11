namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// A transition in a <see cref="Nfa{TResult}"/>
    /// </summary>
    internal sealed class NfaTransition
    {
        /// <summary>
        /// The first character that triggers this transition.
        /// </summary>
        public readonly char FirstChar;

        /// <summary>
        /// The last character that triggers this transition.
        /// </summary>
        public readonly char LastChar;

        /// <summary>
        /// The target state of this transition.
        /// </summary>
        public readonly int State;

        /// <summary>
        /// Creates a new immutable NFA transtition.
        /// </summary>
        /// <param name="firstChar">The first character that triggers this transition.</param>
        /// <param name="lastChar">The last character that triggers this transition.</param>
        /// <param name="state">The target state of this transition.</param>
        public NfaTransition(char firstChar, char lastChar, int state)
        {
            FirstChar = firstChar;
            LastChar = lastChar;
            State = state;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is NfaTransition t)
            {
                return FirstChar == t.FirstChar && LastChar == t.LastChar && State == t.State;
            }

            return false;
        }

        public override int GetHashCode()
        {
            var hash = unchecked((int) 2166136261L);
            hash = (hash ^ FirstChar) * 16777619;
            hash = (hash ^ LastChar) * 16777619;
            hash = (hash ^ State) * 16777619;
            return hash ^ (hash >> 16);
        }
    }
}