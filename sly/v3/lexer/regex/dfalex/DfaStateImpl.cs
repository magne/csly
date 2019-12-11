using System;

namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// Implementation of a Dfa State.
    ///
    /// This can either be a "placeholder" state that delegates to another DFA state, or a DFA state in final form.
    /// As the last step in DFA construction,
    /// </summary>
    [Serializable]
    internal abstract class DfaStateImpl<TResult> : DfaState<TResult>
    {
        /// <summary>
        /// Replace any internal placeholder references with references to their delegates.
        ///
        /// Every reference to a state X is replaces with x.resolvePlaceholder();
        /// </summary>
        internal abstract void FixPlaceholderReferences();

        /// <summary>
        /// If this is a placeholder that delegates to another state, return that other state.  Otherwise return this.
        ///
        /// This method will follow a chain of placeholders to the end
        /// </summary>
        /// <returns>the final delegate of this state</returns>
        internal abstract DfaStateImpl<TResult> ResolvePlaceholder();
    }
}