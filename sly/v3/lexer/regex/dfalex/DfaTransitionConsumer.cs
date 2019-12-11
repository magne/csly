namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// Accept a DFA transition.
    ///
    /// This call indicates that the current state has a transition to target on every character with code point
    /// &gt;= firstChar and &lt;= lastChar
    /// </summary>
    /// <param name="firstChar">First character that triggers this transition</param>
    /// <param name="lastChar">Last character that triggers this transition</param>
    /// <param name="target">Target state of this transition</param>
    internal delegate void DfaTransitionConsumer<TResult>(char firstChar, char lastChar, DfaState<TResult> target);
}