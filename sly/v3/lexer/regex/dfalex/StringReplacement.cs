namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// Functional interface that provides the replacement values for strings in a search+replace operation of patterns found in a string.
    /// </summary>
    /// <param name="dest">The replacement text for the matching substring should be written here</param>
    /// <param name="src">The string being searched, or the part of the stream being searched that contains the current match</param>
    /// <param name="startPos">the start index of the current match in src</param>
    /// <param name="endPos">the end index of the current match in src</param>
    /// <returns>f this is &gt;0, then it is the position in the source string at which to continue processing after replacement.
    /// If you set this &lt;= startPos, an IndexOutOfBoundsException will be thrown to abort the infinite loop that would result.  Almost always return 0.</returns>
    internal delegate int StringReplacement(IAppendable dest, string src, int startPos, int endPos);
}