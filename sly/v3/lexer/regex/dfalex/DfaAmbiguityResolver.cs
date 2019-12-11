using System.Collections.Generic;

namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// Implementations of this interface are used to resolve ambiguities in <see cref="DfaBuilder{TResult}"/>.
    ///
    /// When it's possible for a single string to match patterns that produce different results, the ambiguity resolver
    /// is called to determine what the result should be.
    ///
    /// The implementation can throw a <see cref="DfaAmbiguityException"/> in this case, or can combine the multiple
    /// result objects into a single object if its type (e.g., EnumSet) permits.
    /// </summary>
    /// <param name="accepts">The accept results ambiguities to resolve</param>
    /// <typeparam name="TResult">The type of result to produce by matching a pattern.</typeparam>
    internal delegate TResult DfaAmbiguityResolver<TResult>(ISet<TResult> accepts);
}