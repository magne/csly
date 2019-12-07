using System;
using System.Collections.Generic;
using System.Linq;
using sly.v3.lexer.fsm.transitioncheck;

namespace sly.v3.lexer.fsm
{
    internal delegate bool TransitionPrecondition(ReadOnlyMemory<char> value);

    // ReSharper disable once InconsistentNaming
    internal class FSMLexerBuilder<T>
    {
        private readonly List<FSMNode<T>> nodes;

        private readonly Dictionary<string, int> marks;

        private readonly List<List<FSMTransition>> transitions;

        private int currentState;

        private bool ignoreWhiteSpace;

        // ReSharper disable once InconsistentNaming
        private bool ignoreEOL;

        private List<char> whiteSpaces = new List<char>();

        public FSMLexerBuilder()
        {
            nodes = new List<FSMNode<T>>();
            marks = new Dictionary<string, int>();
            transitions = new List<List<FSMTransition>>();

            currentState = AddNode().Id;
            GetNode(currentState).IsStart = true;
        }

        public FSMLexer<T> Build()
        {
            var fsm = new FSMLexer<T>(nodes, transitions)
            {
                IgnoreWhiteSpace = ignoreWhiteSpace,
                IgnoreEOL = ignoreEOL
            };
            foreach (var spaceChar in whiteSpaces)
            {
                fsm.WhiteSpaces.Add(spaceChar);
            }

            return fsm;
        }

        #region FMS

        private bool HasState(int state)
        {
            return 0 <= state && state < nodes.Count;
        }

        private FSMNode<T> GetNode(int state)
        {
            return HasState(state) ? nodes[state] : null;
        }

        private FSMNode<T> AddNode()
        {
            var id = nodes.Count;
            var node = new FSMNode<T>(id);
            nodes.Add(node);
            transitions.Add(null);
            return node;
        }

        internal bool HasCallback(int nodeId)
        {
            var node = GetNode(nodeId);
            return node != null && node.HasCallback;
        }

        private void SetCallback(int nodeId, NodeCallback<T> callback)
        {
            var node = GetNode(nodeId);
            if (node != null)
            {
                node.Callback = callback;
            }
        }

        private FSMTransition GetTransition(int nodeId, char token)
        {
            FSMTransition transition = null;
            if (HasState(nodeId))
            {
                var leavingTransitions = transitions[nodeId];
                if (leavingTransitions != null)
                {
                    transition = leavingTransitions.FirstOrDefault(t => t.Match(token));
                }
            }

            return transition;
        }

        private int AddTransition(AbstractTransitionCheck check, int fromNode, int toNode)
        {
            if (transitions[fromNode] == null)
            {
                transitions[fromNode] = new List<FSMTransition>();
            }

            if (!HasState(toNode))
            {
                AddNode();
            }

            var transition = new FSMTransition(check, fromNode, toNode);
            transitions[fromNode].Add(transition);

            return toNode;
        }

        #endregion

        #region MARKS

        public FSMLexerBuilder<T> GoTo(int state)
        {
            if (HasState(state))
                currentState = state;
            else
                throw new ArgumentException($"state {state} does not exist in lexer FSM");
            return this;
        }

        public FSMLexerBuilder<T> GoTo(string mark)
        {
            if (marks.ContainsKey(mark))
            {
                return GoTo(marks[mark]);
            }

            throw new ArgumentException($"mark {mark} does not exist in current builder");
        }

        public FSMLexerBuilder<T> Mark(string mark)
        {
            marks[mark] = currentState;
            GetNode(currentState).Mark = mark;
            return this;
        }

        public FSMNode<T> GetNode(string mark)
        {
            FSMNode<T> node = null;
            if (marks.TryGetValue(mark, out var nodeId))
            {
                node = GetNode(nodeId);
            }

            return node;
        }

        #endregion

        #region special chars

        // ReSharper disable once InconsistentNaming
        public FSMLexerBuilder<T> IgnoreWS(bool ignore = true)
        {
            ignoreWhiteSpace = ignore;
            return this;
        }

        // ReSharper disable once InconsistentNaming
        public FSMLexerBuilder<T> IgnoreEOL(bool ignore = true)
        {
            ignoreEOL = ignore;
            return this;
        }

        public FSMLexerBuilder<T> WhiteSpace(char spaceChar)
        {
            whiteSpaces.Add(spaceChar);
            return this;
        }

        public FSMLexerBuilder<T> WhiteSpace(char[] spaceChars)
        {
            if (spaceChars != null)
            {
                foreach (var spaceChar in spaceChars)
                {
                    whiteSpaces.Add(spaceChar);
                }
            }

            return this;
        }

        #endregion

        #region NODES

        public FSMLexerBuilder<T> End(T nodeValue)
        {
            if (HasState(currentState))
            {
                var node = GetNode(currentState);
                node.End(nodeValue);
            }

            return this;
        }

        public FSMLexerBuilder<T> CallBack(NodeCallback<T> callback)
        {
            if (HasState(currentState))
            {
                SetCallback(currentState, callback);
            }

            return this;
        }

        #endregion

        #region TRANSITIONS

        public FSMLexerBuilder<T> SafeTransition(char input, TransitionPrecondition precondition = null)
        {
            var transition = GetTransition(currentState, input);
            if (transition != null)
            {
                currentState = transition.ToNode;
            }
            else
            {
                var toNode = AddNode();
                return TransitionTo(input, toNode.Id, precondition);
            }

            return this;
        }

        public FSMLexerBuilder<T> Transition(char input, TransitionPrecondition precondition = null)
        {
            var toNode = AddNode();
            return TransitionTo(input, toNode.Id, precondition);
        }

        public FSMLexerBuilder<T> ConstantTransition(string constant, TransitionPrecondition precondition = null)
        {
            var c = constant[0];
            SafeTransition(c, precondition);
            for (var i = 1; i < constant.Length; i++)
            {
                c = constant[i];
                SafeTransition(c);
            }

            return this;
        }

        private static (string constant, List<(char start, char end)> ranges) ParseRepeatedPattern(string pattern)
        {
            var toParse = pattern;
            if (toParse.StartsWith("[") && toParse.EndsWith("]"))
            {
                var ranges = new List<(char start, char end)>();
                toParse = toParse.Substring(1, toParse.Length - 2);
                var rangesItems = toParse.Split(',');

                var isPattern = true;
                for (var i = 0; i < rangesItems.Length && isPattern; i++)
                {
                    var item = rangesItems[i];
                    isPattern = item.Length == 3 && item[1] == '-';
                    if (isPattern)
                    {
                        ranges.Add((item[0], item[2]));
                    }
                }

                if (isPattern)
                {
                    return (null, ranges);
                }
            }

            return (pattern, null);
        }

        public FSMLexerBuilder<T> RepetitionTransition(int count,
                                                       string pattern,
                                                       TransitionPrecondition precondition = null)
        {
            var (_, ranges) = ParseRepeatedPattern(pattern);

            if (count > 0 && !string.IsNullOrEmpty(pattern))
            {
                if (ranges != null && ranges.Any())
                {
                    for (var i = 0; i < count; i++)
                    {
                        MultiRangeTransition(precondition, ranges.ToArray());
                    }
                }
                else
                {
                    ConstantTransition(pattern, precondition);
                    for (var i = 1; i < count; i++)
                    {
                        ConstantTransition(pattern);
                    }

                    ConstantTransition(pattern, precondition);
                }
            }

            return this;
        }

        public FSMLexerBuilder<T> RangeTransition(char start, char end, TransitionPrecondition precondition = null)
        {
            var toNode = AddNode();
            return RangeTransitionTo(start, end, toNode.Id, precondition);
        }

        public FSMLexerBuilder<T> MultiRangeTransition(params (char start, char end)[] ranges)
        {
            var toNode = AddNode();
            return MultiRangeTransitionTo(toNode.Id, ranges);
        }

        public FSMLexerBuilder<T> MultiRangeTransition(TransitionPrecondition precondition, params (char start, char end)[] ranges)
        {
            var toNode = AddNode();
            return MultiRangeTransitionTo(toNode.Id, precondition, ranges);
        }

        public FSMLexerBuilder<T> ExceptTransition(char[] exceptions, TransitionPrecondition precondition = null)
        {
            var toNode = AddNode();
            return ExceptTransitionTo(exceptions, toNode.Id, precondition);
        }

        public FSMLexerBuilder<T> AnyTransition(char input, TransitionPrecondition precondition = null)
        {
            var toNode = AddNode();
            return AnyTransitionTo(input, toNode.Id, precondition);
        }

        #endregion

        #region DIRECTED TRANSITIONS

        public FSMLexerBuilder<T> TransitionTo(char input, int toNode, TransitionPrecondition precondition = null)
        {
            AbstractTransitionCheck checker = new TransitionSingle(input, precondition);
            currentState = AddTransition(checker, currentState, toNode);
            return this;
        }

        public FSMLexerBuilder<T> RepetitionTransitionTo(string toNodeMark,
                                                         int count,
                                                         string pattern,
                                                         TransitionPrecondition precondition = null)
        {
            var toNode = marks[toNodeMark];
            return RepetitionTransitionTo(toNode, count, pattern, precondition);
        }

        public FSMLexerBuilder<T> RepetitionTransitionTo(int toNode,
                                                         int count,
                                                         string pattern,
                                                         TransitionPrecondition precondition = null)
        {
            var parsedPattern = ParseRepeatedPattern(pattern);

            if (count > 0 && !string.IsNullOrEmpty(pattern))
            {
                if (parsedPattern.ranges != null && parsedPattern.ranges.Any())
                {
                    for (var i = 0; i < count - 1; i++)
                    {
                        MultiRangeTransition(precondition, parsedPattern.ranges.ToArray());
                    }

                    MultiRangeTransitionTo(toNode, precondition, parsedPattern.ranges.ToArray());
                }
                else
                {
                    ConstantTransition(pattern, precondition);
                    for (var i = 1; i < count; i++) ConstantTransition(pattern);
                    ConstantTransition(pattern, precondition);
                }
            }

            return this;
        }

        public FSMLexerBuilder<T> RangeTransitionTo(char start,
                                                    char end,
                                                    int toNode,
                                                    TransitionPrecondition precondition = null)
        {
            AbstractTransitionCheck checker = new TransitionRange(start, end, precondition);
            currentState = AddTransition(checker, currentState, toNode);
            return this;
        }

//        #region multi range directed

        public FSMLexerBuilder<T> MultiRangeTransitionTo(int toNode, params (char start, char end)[] ranges)
        {
            AbstractTransitionCheck checker = new TransitionMultiRange(ranges);
            currentState = AddTransition(checker, currentState, toNode);
            return this;
        }

        public FSMLexerBuilder<T> MultiRangeTransitionTo(int toNode,
                                                         TransitionPrecondition precondition = null,
                                                         params (char start, char end)[] ranges)
        {
            AbstractTransitionCheck checker = new TransitionMultiRange(precondition, ranges);
            currentState = AddTransition(checker, currentState, toNode);
            return this;
        }

        public FSMLexerBuilder<T> MultiRangeTransitionTo(string toNodeMark, params (char start, char end)[] ranges)
        {
            var toNode = marks[toNodeMark];
            return MultiRangeTransitionTo(toNode, ranges);
        }

//        #endregion

        public FSMLexerBuilder<T> ExceptTransitionTo(char[] exceptions, string toNodeMark, TransitionPrecondition precondition = null)
        {
            var toNode = marks[toNodeMark];
            return ExceptTransitionTo(exceptions, toNode, precondition);
        }

        public FSMLexerBuilder<T> ExceptTransitionTo(char[] exceptions, int toNode, TransitionPrecondition precondition = null)
        {
            AbstractTransitionCheck checker = new TransitionAnyExcept(exceptions, precondition);
            currentState = AddTransition(checker, currentState, toNode);
            return this;
        }

        public FSMLexerBuilder<T> AnyTransitionTo(char input, string toNodeMark, TransitionPrecondition precondition = null)
        {
            var toNode = marks[toNodeMark];
            return AnyTransitionTo(input, toNode, precondition);
        }

        public FSMLexerBuilder<T> AnyTransitionTo(char input, int toNode, TransitionPrecondition precondition = null)
        {
            AbstractTransitionCheck checker = new TransitionAny(precondition);
            currentState = AddTransition(checker, currentState, toNode);
            return this;
        }

        public FSMLexerBuilder<T> TransitionTo(char input, string toNodeMark, TransitionPrecondition precondition = null)
        {
            var toNode = marks[toNodeMark];
            return TransitionTo(input, toNode, precondition);
        }

        public FSMLexerBuilder<T> RangeTransitionTo(char start,
                                                    char end,
                                                    string toNodeMark,
                                                    TransitionPrecondition precondition = null)
        {
            var toNode = marks[toNodeMark];
            return RangeTransitionTo(start, end, toNode, precondition);
        }

        #endregion
    }
}