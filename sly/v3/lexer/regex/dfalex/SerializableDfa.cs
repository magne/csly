using System;
using System.Collections.Generic;

namespace sly.v3.lexer.regex.dfalex
{
    [Serializable]
    internal class SerializableDfa<TResult>
    {
        private readonly List<DfaStatePlaceholder<TResult>> dfaStates;
        private readonly int[]                              startStateNumbers;

        private List<DfaState<TResult>> startStatesMemo;

        public SerializableDfa(RawDfa<TResult> rawDfa)
        {
            List<DfaStateInfo> origStates = rawDfa.States;
            int len = origStates.Count;
            dfaStates = new List<DfaStatePlaceholder<TResult>>(len);
            startStateNumbers = rawDfa.StartStates;
            while (dfaStates.Count < len)
            {
                dfaStates.Add(new PackedTreeDfaPlaceholder<TResult>(rawDfa, dfaStates.Count));
            }
        }

        public List<DfaState<TResult>> GetStartStates()
        {
            if (startStatesMemo == null)
            {
                int len = dfaStates.Count;
                for (int i = 0; i < len; ++i)
                {
                    dfaStates[i].CreateDelegate(i, dfaStates);
                }

                for (int i = 0; i < len; ++i)
                {
                    dfaStates[i].FixPlaceholderReferences();
                }

                startStatesMemo = new List<DfaState<TResult>>(startStateNumbers.Length);
                foreach (int startState in startStateNumbers)
                {
                    startStatesMemo.Add(dfaStates[startState].ResolvePlaceholder());
                }
            }

            return startStatesMemo;
        }
    }
}