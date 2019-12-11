using System;
using System.Collections.Generic;

namespace sly.v3.lexer.regex.dfalex
{
    internal class DfaStateInfo
    {
        private readonly int             acceptSetIndex;
        private readonly int             transitionCount;
        private readonly NfaTransition[] transitionBuf;

        internal DfaStateInfo(List<NfaTransition> transitions, int acceptSetIndex)
        {
            this.acceptSetIndex = acceptSetIndex;
            transitionCount = transitions.Count;
            transitionBuf = transitions.ToArray();
        }

        public int GetAcceptSetIndex()
        {
            return acceptSetIndex;
        }

        public int GetTransitionCount()
        {
            return transitionCount;
        }

        public NfaTransition GetTransition(int index)
        {
            return transitionBuf[index];
        }

        public void ForEachTransition(Action<NfaTransition> consumer)
        {
            for (var i = 0; i < transitionCount; ++i)
            {
                consumer(transitionBuf[i]);
            }
        }
    }
}