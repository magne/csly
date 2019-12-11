using System;
using System.Collections.Generic;

namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// A state in a char-matching deterministic finite automaton (that's the google phrase) or DFA.
    /// </summary>
    /// <typeparam name="TResult">The type of result produced by matching a pattern.</typeparam>
    [Serializable]
    internal abstract class DfaState<TResult>
    {
        /// <summary>
        /// Process acharacter and get the next state.
        /// </summary>
        /// <param name="ch">input character</param>
        /// <returns>The DfaState that ch transitions to from this one, or null if there is no such state</returns>
        public abstract DfaState<TResult> GetNextState(char ch);

        /// <summary>
        /// Get the result that has been matched if we've transitioned into this state.
        /// </summary>
        /// <returns>If the sequence of characters that led to this state match a pattern in the language being processed,
        /// the match result for that pattern is returned. Otherwise null.</returns>
        public abstract TResult GetMatch();

        /// <summary>
        /// Get the state number.  All states reachable from the output of a single call to a {@link DfaBuilder} build
        /// method will be compactly numbered starting at 0.
        ///
        /// These state numbers can be used to maintain auxiliary information about a DFA.
        ///
        /// See {@link DfaAuxiliaryInformation}
        /// </summary>
        /// <returns>this state's state number</returns>
        public abstract int GetStateNumber();

        /// <summary>
        /// Enumerate all the transitions out of this state
        /// </summary>
        /// <param name="consumer">each DFA transition will be sent here</param>
        public abstract void EnumerateTransitions(DfaTransitionConsumer<TResult> consumer);

        /// <summary>
        /// Get an {@link Iterable} of all the successor states of this state.
        ///
        /// Note that the same successor state may appear more than once in the interation
        /// </summary>
        /// <returns>an iterable of successor states.</returns>
        public abstract IEnumerable<DfaState<TResult>> GetSuccessorStates();

        /// <summary>
        ///
        /// </summary>
        /// <returns>true if this state has any successor states</returns>
        public abstract bool HasSuccessorStates();
    }
}