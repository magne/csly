namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// Base interface for the types of patterns that can be used with <see cref="DfaBuilder{TResult}"/> to specify a set of strings
    /// to match.
    ///
    /// The primary implementation classes are <see cref="Pattern"/> and <see cref="CharRange"/>.
    /// </summary>
    internal interface IMatchable
    {
        /// <summary>
        /// Add states to an NFA to match the desired pattern.
        ///
        /// New states will be created in the NFA to match the pattern and transitions to the given <paramref name="targetState"/>.
        ///
        /// NO NEW TRANSITIONS will be added to the target state or any other pre-existing states.
        /// </summary>
        /// <param name="nfa">NFA to add to.</param>
        /// <param name="targetState">target state after the pattern is matched</param>
        /// <typeparam name="TResult">The type of result produced by matching a pattern.</typeparam>
        /// <returns>a state that transitions to <paramref name="targetState"/> after matching the pattern, and only after
        /// matching the pattern. This may be <paramref name="targetState"/> if the pattern is an empty string.</returns>
        int AddToNfa<TResult>(Nfa<TResult> nfa, int targetState);

        /// <returns><c>true</c> if this pattern matches the empty string</returns>
        bool MatchesEmpty { get; }

        /// <returns>True if this pattern matches any non-empty string</returns>
        bool MatchesNonEmpty { get; }

        /// <returns>True if this pattern matches anything at all</returns>
        bool MatchesSomething { get; }

        /// <returns>True if this pattern matches an infinite number of strings</returns>
        bool IsUnbounded { get; }

        /// <summary>
        /// Get the reverse of this pattern.
        ///
        /// The reverse of a pattern matches the reverse of all the strings that this pattern matches.
        /// </summary>
        /// <returns>The reverse of this pattern</returns>
        IMatchable Reversed { get; }
    }
}