using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// Simple non-deterministic finite automaton (NFA) representation.
    ///
    /// A set of <see cref="IMatchable"/> patterns is converted to an NFA as an intermediate step toward creating the DFA.
    /// </summary>
    /// <typeparam name="TResult">The type of result produced by matching a pattern.</typeparam>
    internal class Nfa<TResult>
    {
        private readonly List<List<NfaTransition>> stateTransitions = new List<List<NfaTransition>>();
        private readonly List<List<int>>           stateEpsilons    = new List<List<int>>();
        private readonly List<TResult>             stateAccepts     = new List<TResult>();

        /// <summary>
        /// Get the number of states in the NFA
        /// </summary>
        /// <returns>the total number of states that have been added with <see cref="AddState"/></returns>
        public int NumStates()
        {
            return stateAccepts.Count;
        }

        /// <summary>
        /// Add a new state to the NFA.
        /// </summary>
        /// <param name="accept">Add a new state to the NFA</param>
        /// <returns>the number of the new state</returns>
        public int AddState(TResult accept = default)
        {
            var state = stateAccepts.Count;
            stateAccepts.Add(accept);
            stateTransitions.Add(null);
            stateEpsilons.Add(null);
            Debug.Assert(stateAccepts.Count == stateTransitions.Count);
            Debug.Assert(stateAccepts.Count == stateEpsilons.Count);
            return state;
        }

        /// <summary>
        /// Add a transition to the NFA.
        /// </summary>
        /// <param name="from">The state to transition from</param>
        /// <param name="to">The state to transition to</param>
        /// <param name="firstChar">The first character in the accepted range</param>
        /// <param name="lastChar">The last character in the accepted range</param>
        public void AddTransition(int from, int to, char firstChar, char lastChar)
        {
            var list = stateTransitions[from];
            if (list == null)
            {
                list = new List<NfaTransition>();
                stateTransitions[from] = list;
            }

            list.Add(new NfaTransition(firstChar, lastChar, to));
        }

        /// <summary>
        /// Add an epsilon transition to the NFA.
        /// </summary>
        /// <param name="from">The state to transition from</param>
        /// <param name="to">The state to transition to</param>
        public void AddEpsilon(int from, int to)
        {
            var list = stateEpsilons[from];
            if (list == null)
            {
                list = new List<int>();
                stateEpsilons[from] = list;
            }

            list.Add(to);
        }

        /// <summary>
        /// Get the result attached to the given state
        /// </summary>
        /// <param name="state">the state number</param>
        /// <returns>the result that was provided to <see cref="AddState"/> when the state was created</returns>
        public TResult GetAccept(int state)
        {
            return stateAccepts[state];
        }

        /// <summary>
        /// Check whether a state has any non-epsilon transitions or has a result attached
        /// </summary>
        /// <param name="state">the state number</param>
        /// <returns>true if the state has any transitions or accepts</returns>
        public bool HasTransitionsOrAccepts(int state)
        {
            return !EqualityComparer<TResult>.Default.Equals(stateAccepts[state], default) || stateTransitions[state] != null;
        }

        /// <summary>
        /// Get all the epsilon transitions from a state
        /// </summary>
        /// <param name="state">the state number</param>
        /// <returns>An enumerable over all transitions out of the given state</returns>
        public IEnumerable<int> GetStateEpsilons(int state)
        {
            var list = stateEpsilons[state];
            return list != null ? (IEnumerable<int>) list : new int[0];
        }

        /// <summary>
        /// Get all the non-epsilon transitions from a state
        /// </summary>
        /// <param name="state">the state number</param>
        /// <returns>An enumerable over all transitions out of the given state</returns>
        public IEnumerable<NfaTransition> GetStateTransitions(int state)
        {
            var list = stateTransitions[state];
            return list != null ? (IEnumerable<NfaTransition>) list : new NfaTransition[0];
        }

        /// <summary>
        /// Make modified state, if necessary, that doesn't match the empty string.
        ///
        /// If <tt>state</tt> has a non-null result attached, or can reach such a state through epsilon transitions,
        /// then a DFA made from that state would match the empty string.  In that case a new NFA state will be created
        /// that matches all the same strings <i>except</i> the empty string.
        /// </summary>
        /// <param name="state">the number of the state to disemptify</param>
        /// <returns>If <tt>state</tt> matches the empty string, then a new state that does not match the empty string
        /// is returned.  Otherwise <tt>state</tt> is returned.</returns>
        public int Disemptify(int state)
        {
            var reachable = new List<int>();

            //first find all epsilon-reachable states
            {
                var checkSet = new HashSet<int>();
                reachable.Add(state);
                checkSet.Add(reachable[0]); //same Integer instance
                for (var i = 0; i < reachable.Count; ++i)
                {
                    ForStateEpsilons(reachable[i],
                                     num =>
                                     {
                                         if (checkSet.Add(num))
                                         {
                                             reachable.Add(num);
                                         }
                                     });
                }
            }

            //if none of them accept, then we're done
            for (var i = 0;; ++i)
            {
                if (i >= reachable.Count)
                {
                    return state;
                }

                if (!GetAccept(reachable[i]).Equals(default))
                {
                    break;
                }
            }

            //need to make a new disemptified state.  first get all transitions
            var newState = AddState();
            ISet<NfaTransition> transSet = new HashSet<NfaTransition>();
            foreach (var src in reachable)
            {
                ForStateTransitions(src,
                                    trans =>
                                    {
                                        if (transSet.Add(trans))
                                        {
                                            AddTransition(newState, trans.State, trans.FirstChar, trans.LastChar);
                                        }
                                    });
            }

            return newState;
        }

        internal void ForStateEpsilons(int state, Action<int> dest)
        {
            var list = stateEpsilons[state];
            list?.ForEach(dest);
        }

        internal void ForStateTransitions(int state, Action<NfaTransition> dest)
        {
            var list = stateTransitions[state];
            list?.ForEach(dest);
        }
    }
}