using System;
using System.Collections.Generic;

namespace sly.v3.lexer.regex.dfalex
{

    /// <summary>
    /// Base class for serializable placeholders that construct final-form DFA states and
    /// temporarily assume their places in the DFA.
    ///
    /// In serialized placeholders, target states are identified by their state number in a SerializableDfa.
    /// </summary>
    [Serializable]
    internal abstract class DfaStatePlaceholder<TResult> : DfaStateImpl<TResult>
    {
        protected DfaStateImpl<TResult> Delegate = null;

        /// <summary>
        /// Create a new DfaStatePlaceholder
        ///
        /// The initially constructed stat will accept no strings.
        /// </summary>
        public DfaStatePlaceholder()
        { }

        /// <summary>
        /// Creates the final form delegate state, implementing all the required transitions and matches.
        ///
        /// This is called on all DFA state placeholders after they are constructed
        /// </summary>
        /// <param name="statenum"></param>
        /// <param name="allStates"></param>
        internal abstract void CreateDelegate(int statenum, List<DfaStatePlaceholder<TResult>> allStates);

        internal sealed override void FixPlaceholderReferences()
        {
            Delegate.FixPlaceholderReferences();
        }

        internal sealed override DfaStateImpl<TResult> ResolvePlaceholder()
        {
            return Delegate.ResolvePlaceholder();
        }

        public sealed override DfaState<TResult> GetNextState(char c)
        {
            return Delegate.GetNextState(c);
        }

        public sealed override TResult GetMatch()
        {
            return Delegate.GetMatch();
        }

        public sealed override void EnumerateTransitions(DfaTransitionConsumer<TResult> consumer)
        {
            Delegate.EnumerateTransitions(consumer);
        }

        public sealed override int GetStateNumber()
        {
            return Delegate.GetStateNumber();
        }

        public override IEnumerable<DfaState<TResult>> GetSuccessorStates()
        {
            return Delegate.GetSuccessorStates();
        }

        public override bool HasSuccessorStates()
        {
            return Delegate.HasSuccessorStates();
        }
    }
}