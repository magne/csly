using System.Collections.Generic;

namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    ///A DFA in uncomrpessed form
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal class RawDfa<TResult>
    {
        private readonly List<DfaStateInfo> dfaStates;
        private readonly List<TResult>      acceptSets;
        private readonly int[]              startStates;

        /**
	 * Create a new RawDfa.
	 */
        public RawDfa(List<DfaStateInfo> dfaStates,
                      List<TResult> acceptSets,
                      int[] startStates)
        {
            this.dfaStates = dfaStates;
            this.acceptSets = acceptSets;
            this.startStates = startStates;
        }

        public List<DfaStateInfo> States => dfaStates;

        public List<TResult> AcceptSets => acceptSets;

        public int[] StartStates => startStates;
    }
}