using System;
using System.Collections.Generic;
using System.Linq;
using sly.v3.lexer.regex.dfalex;
using Xunit;

namespace ParserTests.v3.lexer.regex.dfalex
{
    /// <summary>
    /// Test DFA cycles and destiny finding
    /// </summary>
    public class TarjanTest
    {
        private Random      r        = new Random(0x4D45524D);
        private List<State> m_states = new List<State>();
        private int[]       m_cycleNumbers;

        [Fact]
        public void Test()
        {
            //tested up to 10000 once, but that takes a very long time (O(n^3))
            for (var n = 0; n < 500; n++)
            {
                var pcycle = r.NextDouble();
                pcycle *= pcycle * pcycle;
                var plink = r.NextDouble();
                var paccept = r.NextDouble();
                RandomDfa(n, pcycle, plink, paccept);
                Check();
            }
        }

        private void RandomDfa(int nstates, double Pcycle, double Plink, double Paccept)
        {
            m_cycleNumbers = new int[nstates];
            m_states.Clear();
            var cycleCounter = 0;
            while (m_states.Count < nstates)
            {
                var pos = m_states.Count;
                var cycSz = 1;
                if (r.NextDouble() < Pcycle)
                {
                    cycSz = Math.Min(r.Next(20) + 1, nstates - pos);
                }

                for (var i = 0; i < cycSz; ++i)
                {
                    int? accept = null;
                    if (r.NextDouble() < Paccept)
                    {
                        accept = r.Next(8);
                    }

                    m_states.Add(new State(pos + i, accept));
                }

                if (cycSz > 1)
                {
                    for (var i = 0; i < cycSz; ++i)
                    {
                        m_cycleNumbers[pos + i] = cycleCounter;
                        if (i != 0)
                        {
                            m_states[pos + i].Link(m_states[pos + i - 1]);
                        }
                    }

                    m_states[pos].Link(m_states[pos + cycSz - 1]);
                    ++cycleCounter;
                }
                else
                {
                    m_cycleNumbers[pos] = -1;
                }
            }

            //link
            for (var pos = 1; pos < nstates; ++pos)
            {
                var nLinks = (int) Math.Round(Plink * pos);
                for (var i = 0; i < nLinks; i++)
                {
                    var target = r.Next(pos);
                    m_states[pos].Link(m_states[target]);
                }
            }

            for (var pos = 0; pos < nstates; ++pos)
            {
                m_states[pos].MoveLink0(r);
            }
        }

        private void Check()
        {
            var nStates = m_states.Count;
            var starts = new List<DfaState<int?>>();
            //find roots that cover all the states
            {
                var reached = new bool[m_states.Count];
                for (var i = 0; i < nStates; i++)
                {
                    var src = m_states[i];
                    foreach (var dest in src.GetSuccessorStates())
                    {
                        if (m_cycleNumbers[src.Number] != m_cycleNumbers[dest.GetStateNumber()])
                        {
                            System.Diagnostics.Debug.Assert(dest.GetStateNumber() < src.Number);
                            reached[dest.GetStateNumber()] = true;
                        }
                    }
                }

                for (var i = nStates - 2; i >= 0; --i)
                {
                    if (m_cycleNumbers[i] >= 0 && m_cycleNumbers[i] == m_cycleNumbers[i + 1] && reached[i + 1])
                    {
                        reached[i] = true;
                    }
                }

                for (var i = 0; i < nStates; i++)
                {
                    if (i == 0 || m_cycleNumbers[i] < 0 || m_cycleNumbers[i] != m_cycleNumbers[i - 1])
                    {
                        if (!reached[i])
                        {
                            starts.Add(m_states[i]);
                        }
                    }
                }
            }

            var auzInfo = new DfaAuxiliaryInformation<int?>(starts);
            var gotCycles = auzInfo.GetCycleNumbers();
            Assert.Equal(nStates, gotCycles.Length);
            for (var i = 0; i < nStates; i++)
            {
                if (m_cycleNumbers[i] < 0)
                {
                    Assert.True(gotCycles[i] < 0);
                }
                else
                {
                    Assert.True(gotCycles[i] >= 0);
                    if (i > 0)
                    {
                        Assert.Equal(m_cycleNumbers[i] == m_cycleNumbers[i - 1], gotCycles[i] == gotCycles[i - 1]);
                    }
                }
            }
        }

        private class State : DfaState<int?>
        {
            private readonly  List<DfaState<int?>> transitions = new List<DfaState<int?>>();
            private readonly  int?                 accept;
            internal readonly int                  Number;

            public State(int number, int? accept)
            {
                this.accept = accept;
                Number = number;
            }

            public void Link(DfaState<int?> target)
            {
                transitions.Add(target);
            }

            public void MoveLink0(Random r)
            {
                if (transitions.Count > 1)
                {
                    var d = r.Next(transitions.Count);
                    if (d != 0)
                    {
                        var t = transitions[d];
                        transitions[d] = transitions[0];
                        transitions[0] = t;
                    }
                }
            }

            public override DfaState<int?> GetNextState(char ch)
            {
                if (ch <= transitions.Count)
                {
                    return transitions[ch];
                }

                return null;
            }

            public override int? GetMatch()
            {
                return accept;
            }

            public override int GetStateNumber()
            {
                return Number;
            }

            public override void EnumerateTransitions(DfaTransitionConsumer<int?> consumer)
            {
                for (var i = 0; i < transitions.Count; ++i)
                {
                    consumer((char) i, (char) i, transitions[i]);
                }
            }

            public override IEnumerable<DfaState<int?>> GetSuccessorStates()
            {
                return transitions;
            }

            public override bool HasSuccessorStates()
            {
                return transitions.Any();
            }
        }
    }
}