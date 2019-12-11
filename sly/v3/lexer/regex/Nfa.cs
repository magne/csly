using System.Collections.Generic;
using System.Linq;

namespace sly.v3.lexer.regex
{
    // ----------------------------------------------------------------------

    // Regular expressions, NFAs, DFAs, and dot graphs
    // sestoft@itu.dk
    // Java 2001-07-10 * C# 2001-10-22 * Gen C# 2001-10-23, 2003-09-03

    // ----------------------------------------------------------------------

    /*
     * Class Nfa and conversion from NFA to DFA
     *
     * A nondeterministic finite automaton (NFA) is represented as a Map from state number (int) to a List of Transitions, a Transition being a pair
     * of a label lab (a String, null meaning epsilon) and a target state (an int).
     *
     * A DFA is created from an NFA in two steps:
     *
     *     (1) Construct a DFA whose each of whose states is composite, namely a set of NFA states (Set of int).  This is done by methods
     *         CompositeDfaTrans and EpsilonClose.
     *
     *     (2) Replace composite states (Set of int) by simple states (int).  This is done by methods Rename and MkRenamer.
     *
     * Method CompositeDfaTrans works as follows:
     *
     *     Create the epsilon-closure S0 (a Set of ints) of the start state s0, and put it in a worklist (a Queue).  Create an empty DFA transition
     *     relation, which is a Map from a composite state (an epsilon-closed Set of ints) to a Map from a label (a non-null String) to a composite
     *     state.
     *
     *     Repeatedly choose a composite state S from the worklist.  If it is not already in the keyset of the DFA transition relation, compute for
     *     every non-epsilon label lab the set T of states reachable by that label from some state s in S.  Compute the epsilon-closure Tclose of
     *     every such state T and put it on the worklist.  Then add the transition S -lab-> Tclose to the DFA transition relation, for every lab.
     *
     * Method EpsilonClose works as follows:
     *
     *     Given a set S of states.  Put the states of S in a worklist.  Repeatedly choose a state s from the worklist, and consider all
     *     epsilon-transitions s -eps-> s' from s.  If s' is in S already, then do nothing; otherwise ass s' to S and the worklist.  When the
     *     worklist is empty, S is epsilon-closed; return S.
     *
     * Method MkRenamer works as follows:
     *
     *     Given a Map from Set of int to something, create an injective Map from Set of int to int, by choosing a fresh int for every value of the
     *     map.
     *
     * Method Rename works as follows:
     *     Given a Map from Set of int to Map from String to Set of int, use the result of MkRenamer to replace all Sets of ints by ints.
     */
    internal class Nfa
    {
        private readonly int                                 startState;
        private readonly int                                 exitState; // This is the unique accept state
        private readonly IDictionary<int, IList<Transition>> trans;

        public Nfa(int startState, int exitState)
        {
            this.startState = startState;
            this.exitState = exitState;
            trans = new Dictionary<int, IList<Transition>>();
            if (startState != exitState)
            {
                trans.Add(exitState, new List<Transition>());
            }
        }

        public int Start => startState;

        public int Exit => exitState;

        public IDictionary<int, IList<Transition>> Trans => trans;

        public void AddTrans(int s1, string lab, int s2)
        {
            if (!trans.TryGetValue(s1, out var s1Trans))
            {
                s1Trans = new List<Transition>();
                trans.Add(s1, s1Trans);
            }

            s1Trans.Add(new Transition(lab, s2));
        }

        public void AddTrans(KeyValuePair<int, IList<Transition>> tr)
        {
            // Assumption: if tr is in trans, it maps to an empty list (end state)
            trans.Remove(tr.Key);
            trans.Add(tr.Key, tr.Value);
        }

        public override string ToString()
        {
            return $"NFA start={startState} exit={exitState}";
        }

        // Construct the transition relation of a composite-state DFA from an NFA with start state s0 and transition relation trans (a Map from int to
        // List of Transition).  The start state of the constructed DFA is the epsilon closure of s0, and its transition relation is a Map from a
        // composite state (a Set of ints) to a Map from label (a String) to a composite state (a Set of ints).
        private static IDictionary<ISet<int>, IDictionary<string, ISet<int>>> CompositeDfaTrans(int s0, IDictionary<int, IList<Transition>> trans)
        {
            var S0 = EpsilonClose(new HashSet<int> {s0}, trans);
            var worklist = new Queue<ISet<int>>();
            worklist.Enqueue(S0);
            // The transition relation of the DFA
            var res = new Dictionary<ISet<int>, IDictionary<string, ISet<int>>>(new SetComparer<int>());
            while (worklist.Count != 0)
            {
                var S = worklist.Dequeue();
                if (!res.ContainsKey(S))
                {
                    // The S -lab-> T transition relation being constructed for a given S
                    var STrans = new Dictionary<string, ISet<int>>();
                    // For all s in S, consider all transitions s -lab-> t
                    foreach (var s in S)
                    {
                        // For all non-epsilon transitions s -lab-> t, add t to T
                        foreach (var tr in trans[s])
                        {
                            if (tr.Lab != null)
                            {
                                // Already a transition on lab
                                if (!STrans.TryGetValue(tr.Lab, out var toState))
                                {
                                    // No transitions on lab yet
                                    toState = new HashSet<int>();
                                    STrans.Add(tr.Lab, toState);
                                }

                                toState.Add(tr.Target);
                            }
                        }
                    }

                    // Epsilon-close all T such that S -lab-> T, and put on worklist
                    var STransClosed = new Dictionary<string, ISet<int>>();
                    foreach (var entry in STrans)
                    {
                        var Tclose = EpsilonClose(entry.Value, trans);
                        STransClosed.Add(entry.Key, Tclose);
                        worklist.Enqueue(Tclose);
                    }

                    res.Add(S, STransClosed);
                }
            }

            return res;
        }

        // Compute epsilon-closure of state set S in transition relation trans.
        private static ISet<int> EpsilonClose(ISet<int> S, IDictionary<int, IList<Transition>> trans)
        {
            // The worklist initially contains all S members
            var worklist = new Queue<int>(S);
            var res = S;
            while (worklist.Count != 0)
            {
                var s = worklist.Dequeue();
                foreach (var tr in trans[s])
                {
                    if (tr.Lab == null && !res.Contains(tr.Target))
                    {
                        res.Add(tr.Target);
                        worklist.Enqueue(tr.Target);
                    }
                }
            }

            return res;
        }

        // Compute a renamer, which is a Map from Set of int to int
        private static IDictionary<ISet<int>, int> MkRenamer(IEnumerable<ISet<int>> states)
        {
            var renamer = new Dictionary<ISet<int>, int>(new SetComparer<int>());
            var count = 0;
            foreach (var k in states)
            {
                renamer.Add(k, count++);
            }

            return renamer;
        }

        // Using a renamer (a Map from Set of int to int), replace composite (Set of int) states with simple (int)
        // states in the transition relation trans, which is assumed to be a Map from Set of int to Map from string to
        // Set of int.  The result is a Map from int to Map from string to int.
        private static IDictionary<int, IDictionary<string, int>> Rename(IDictionary<ISet<int>, int> renamer,
            IDictionary<ISet<int>, IDictionary<string, ISet<int>>> trans)
        {
            var newtrans = new Dictionary<int, IDictionary<string, int>>();
            foreach (var entry in trans)
            {
                var k = entry.Key;
                var newktrans = new Dictionary<string, int>();
                foreach (var tr in entry.Value)
                {
                    newktrans.Add(tr.Key, renamer[tr.Value]);
                }

                newtrans.Add(renamer[k], newktrans);
            }

            return newtrans;
        }

        private static ISet<int> AcceptStates(IEnumerable<ISet<int>> states, IDictionary<ISet<int>, int> renamer, int exit)
        {
            var acceptStates = new HashSet<int>();
            foreach (var state in states)
            {
                if (state.Contains(exit))
                {
                    acceptStates.Add(renamer[state]);
                }
            }

            return acceptStates;
        }

        internal Dfa ToDfa()
        {
            var cDfaTrans = CompositeDfaTrans(startState, trans);
            var cDfaStart = EpsilonClose(new HashSet<int> {startState}, trans);
            var cDfaStates = cDfaTrans.Keys;
            var renamer = MkRenamer(cDfaStates);
            var simpleDfaTrans = Rename(renamer, cDfaTrans);
            var simpleDfaStart = renamer[new HashSet<int>(cDfaStart)];
            var simpleDfaAccept = AcceptStates(cDfaStates, renamer, exitState);
            return new Dfa(simpleDfaStart, simpleDfaAccept, simpleDfaTrans);
        }

        // Nested class for creating distinctly named states when constructing NFAs
        internal class NameSource
        {
            private static int nextName;

            public static int Next()
            {
                return nextName++;
            }
        }
    }

    internal sealed class SetComparer<T> : IEqualityComparer<ISet<T>>
    {
        public bool Equals(ISet<T> x, ISet<T> y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.SetEquals(y);
        }

        public int GetHashCode(ISet<T> obj)
        {
            return obj.Aggregate(0, (current, value) => current ^ (value?.GetHashCode() ?? 0) & int.MaxValue);
        }
    }
}