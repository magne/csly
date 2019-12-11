using System;
using System.Collections;
using System.Collections.Generic;

namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// An <see cref="IEnumerator{T}"/> that provides access to the pattern matches in a string.
    ///
    /// <see cref="StringSearcher{TResult}.SearchString"/> produces these.
    /// </summary>
    internal interface IStringMatchEnumerator<TResult> : IEnumerator<TResult>
    {
        /// <summary>
        /// Get the position of the start of the last match in the string.
        /// </summary>
        /// <returns>the index of the first character in the last match</returns>
        /// <exception cref="InvalidOperationException">unless called after a valid call to <see cref="IEnumerator.MoveNext"/></exception>
        int MatchStartPosition { get; }

        /// <summary>
        /// Get the position of the end of the last match in the string.
        /// </summary>
        /// <returns>the index after the last character in the last match</returns>
        /// <exception cref="InvalidOperationException">unless called after a valid call to <see cref="IEnumerator.MoveNext"/></exception>
        int MatchEndPosition { get; }

        /// <summary>
        /// Get the string value of the last match
        ///
        /// Note that a new string is allocated by the first call to this method for each match.
        /// </summary>
        /// <returns>the source portion of the source string corresponding to the last match</returns>
        /// <exception cref="InvalidOperationException">unless called after a valid call to <see cref="IEnumerator.MoveNext"/></exception>
        string MatchValue { get; }

        /// <summary>
        /// Get the result of the last match.
        /// </summary>
        /// <returns>the TResult returned by the last call to <see cref="IEnumerator.MoveNext"/></returns>
        /// <exception cref="InvalidOperationException">unless called after a valid call to <see cref="IEnumerator.MoveNext"/></exception>
        TResult MatchResult { get; }

        /// <summary>
        /// Rewind (or jump forward) to a given position in the source string
        ///
        /// The next match returned will be the one (if any) that starts at a position &gt;= pos
        ///
        /// IMPORTANT:  If this method returns true, you must call <see cref="IEnumerator.MoveNext"/> to get the result
        /// of the next match.  Until then calls to the the match accessor methods will continue to return information
        /// from the previous call to <see cref="IEnumerator.MoveNext"/>.
        /// </summary>
        /// <param name="pos">new position in the source string to search from</param>
        /// <returns>true if there is a match after the given position.  The same value will be returned from <see cref="IEnumerator.MoveNext"/></returns>
        bool Reposition(int pos);
    }
}