namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// For search and replace operations, a functional interface that is called to select replacement text for matches,
    /// based on the TResult.
    ///
    /// This is called by a <see cref="StringSearcher{TResult}.FindAndReplace"/> to replace instances of patterns found
    /// in a string.
    /// </summary>
    /// <param name="dest">The replacement text for the matching substring should be written here</param>
    /// <param name="mr">The TResult produced by the match</param>
    /// <param name="src">The string being searched, or a part of the stream being searched that contains the current match</param>
    /// <param name="startPos">the start index of the current match in src</param>
    /// <param name="endPos">the end index of the current match in src</param>
    /// <returns>if this is &gt;0, then it is the position in the source string at which to continue processing after
    /// replacement.  If you set this &lt;= startPos, a runtime exception will be thrown to abort the infinite loop that
    /// would result.  Almost always return 0.</returns>
    /// <typeparam name="TResult"></typeparam>
    internal delegate int ReplacementSelector<in TResult>(IAppendable dest, TResult mr, string src, int startPos, int endPos);
}