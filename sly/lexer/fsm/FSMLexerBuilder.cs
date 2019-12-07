using System;
using System.Collections.Generic;
using System.Linq;
using sly.lexer.fsm.transitioncheck;

namespace sly.lexer.fsm
{
    public delegate FSMMatch<TLexeme> NodeCallback<TLexeme>(FSMMatch<TLexeme> node);

    public delegate bool TransitionPrecondition(ReadOnlyMemory<char> value);

    // ReSharper disable once InconsistentNaming
    public class FSMLexerBuilder<TNode>
    {
        private int currentState;

        private readonly Dictionary<string, int> marks;


        public FSMLexerBuilder()
        {
            Fsm = new FSMLexer<TNode>();
            currentState = 0;
            marks = new Dictionary<string, int>();
            Fsm.AddNode(default(TNode));
            Fsm.GetNode(0).IsStart = true;
        }

        public FSMLexer<TNode> Fsm { get; }

        #region MARKS

        public FSMLexerBuilder<TNode> GoTo(int state)
        {
            if (Fsm.HasState(state))
                currentState = state;
            else
                throw new ArgumentException($"state {state} does not exist in lexer FSM");
            return this;
        }

        public FSMLexerBuilder<TNode> GoTo(string mark)
        {
            if (marks.ContainsKey(mark))
                GoTo(marks[mark]);
            else
                throw new ArgumentException($"mark {mark} does not exist in current builder");
            return this;
        }


        public FSMLexerBuilder<TNode> Mark(string mark)
        {
            marks[mark] = currentState;
            Fsm.GetNode(currentState).Mark = mark;
            return this;
        }

        public FSMNode<TNode> GetNode(int nodeId)
        {
            return Fsm.GetNode(nodeId);
        }

        public FSMNode<TNode> GetNode(string mark)
        {
            FSMNode<TNode> node = null;
            if (marks.ContainsKey(mark)) node = GetNode(marks[mark]);
            return node;
        }

        #endregion

        #region special chars

        // ReSharper disable once InconsistentNaming
        public FSMLexerBuilder<TNode> IgnoreWS(bool ignore = true)
        {
            Fsm.IgnoreWhiteSpace = ignore;
            return this;
        }

        // ReSharper disable once InconsistentNaming
        public FSMLexerBuilder<TNode> IgnoreEOL(bool ignore = true)
        {
            Fsm.IgnoreEOL = ignore;
            return this;
        }

        public FSMLexerBuilder<TNode> WhiteSpace(char spaceChar)
        {
            Fsm.WhiteSpaces.Add(spaceChar);
            return this;
        }

        public FSMLexerBuilder<TNode> WhiteSpace(char[] spaceChars)
        {
            if (spaceChars != null)
            {
                foreach (var spaceChar in spaceChars)
                {
                    Fsm.WhiteSpaces.Add(spaceChar);
                }
            }

            return this;
        }

        #endregion


        #region NODES

        public FSMLexerBuilder<TNode> End(TNode nodeValue)
        {
            if (Fsm.HasState(currentState))
            {
                var node = Fsm.GetNode(currentState);

                node.IsEnd = true;
                node.Value = nodeValue;
            }

            return this;
        }

        public FSMLexerBuilder<TNode> CallBack(NodeCallback<TNode> callback)
        {
            if (Fsm.HasState(currentState)) Fsm.SetCallback(currentState, callback);

            return this;
        }



        #endregion


        #region TRANSITIONS

        public FSMLexerBuilder<TNode> SafeTransition(char input)
        {
            var transition = Fsm.GetTransition(currentState, input);
            if (transition != null)
                currentState = transition.ToNode;
            else
                return TransitionTo(input, Fsm.NewNodeId);
            return this;
        }

        public FSMLexerBuilder<TNode> SafeTransition(char input, TransitionPrecondition precondition)
        {
            var transition = Fsm.GetTransition(currentState, input);
            if (transition != null)
                currentState = transition.ToNode;
            else
                return TransitionTo(input, Fsm.NewNodeId, precondition);
            return this;
        }


        public FSMLexerBuilder<TNode> Transition(char input)
        {
            return TransitionTo(input, Fsm.NewNodeId);
        }

        public FSMLexerBuilder<TNode> Transition(char input, TransitionPrecondition precondition)
        {
            return TransitionTo(input, Fsm.NewNodeId, precondition);
        }

        public FSMLexerBuilder<TNode> ConstantTransition(string constant, TransitionPrecondition precondition = null)
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


        private (string constant, List<(char start, char end)> ranges) ParseRepeatedPattern(string pattern)
        {
            string toParse = pattern;
            if (toParse.StartsWith("[") && toParse.EndsWith("]"))
            {
                bool isPattern = true;
                List<(char start, char end)> ranges = new List<(char start, char end)>();
                toParse = toParse.Substring(1, toParse.Length - 2);
                var rangesItems = toParse.Split(new char[]{','});
                int i = 0;
                while (i < rangesItems.Length && isPattern)
                {
                    var item = rangesItems[i];
                    isPattern = item.Length == 3 && item[1] == '-';
                    if (isPattern)
                    {
                        ranges.Add((item[0],item[2]));
                    }
                    i++;
                }

                if (isPattern)
                {
                    return (null, ranges);
                }

            }
            return (pattern, null);
        }

        public FSMLexerBuilder<TNode> RepetitionTransition(int count, string pattern,
            TransitionPrecondition precondition = null)
        {
            var parsedPattern = ParseRepeatedPattern(pattern);

            if (count > 0 && !string.IsNullOrEmpty(pattern))
            {
                if (parsedPattern.ranges != null && parsedPattern.ranges.Any())
                {
                    for (int i = 0; i < count; i++)
                    {
                        MultiRangeTransition(precondition, parsedPattern.ranges.ToArray());
                    }
                }
//                if (pattern.StartsWith("[") && pattern.EndsWith("]") && pattern.Contains("-") && pattern.Length == 5)
//                {
//                    var start = pattern[1];
//                    var end = pattern[3];
//                    RangeTransition(start, end, precondition);
//                    for (var i = 1; i < count; i++) RangeTransition(start, end);
//                }
                else
                {
                    ConstantTransition(pattern, precondition);
                    for (var i = 1; i < count; i++) ConstantTransition(pattern);
                    ConstantTransition(pattern, precondition);
                }
            }

            return this;
        }


        public FSMLexerBuilder<TNode> RangeTransition(char start, char end)
        {
            return RangeTransitionTo(start, end, Fsm.NewNodeId);
        }

        public FSMLexerBuilder<TNode> RangeTransition(char start, char end, TransitionPrecondition precondition)
        {
            return RangeTransitionTo(start, end, Fsm.NewNodeId, precondition);
        }

        public FSMLexerBuilder<TNode> MultiRangeTransition(params (char start, char end)[] ranges)
        {
            return MultiRangeTransitionTo(Fsm.NewNodeId,ranges);
        }

        public FSMLexerBuilder<TNode> MultiRangeTransition(TransitionPrecondition precondition , params (char start, char end)[] ranges)
        {
            return MultiRangeTransitionTo(Fsm.NewNodeId, precondition, ranges);
        }



        public FSMLexerBuilder<TNode> ExceptTransition(char[] exceptions)
        {
            return ExceptTransitionTo(exceptions, Fsm.NewNodeId);
        }

        public FSMLexerBuilder<TNode> ExceptTransition(char[] exceptions, TransitionPrecondition precondition)
        {
            return ExceptTransitionTo(exceptions, Fsm.NewNodeId, precondition);
        }

        public FSMLexerBuilder<TNode> AnyTransition(char input)
        {
            return AnyTransitionTo(input, Fsm.NewNodeId);
        }

        public FSMLexerBuilder<TNode> AnyTransition(char input, TransitionPrecondition precondition)
        {
            return AnyTransitionTo(input, Fsm.NewNodeId, precondition);
        }

        #endregion

        #region DIRECTED TRANSITIONS

        public FSMLexerBuilder<TNode> TransitionTo(char input, int toNode)
        {
            AbstractTransitionCheck checker = new TransitionSingle(input);
            if (!Fsm.HasState(toNode)) Fsm.AddNode();
            var transition = new FSMTransition(checker, currentState, toNode);
            Fsm.AddTransition(transition);
            currentState = toNode;
            return this;
        }


        public FSMLexerBuilder<TNode> TransitionTo(char input, int toNode, TransitionPrecondition precondition)
        {
            AbstractTransitionCheck checker = new TransitionSingle(input, precondition);
            if (!Fsm.HasState(toNode)) Fsm.AddNode();
            var transition = new FSMTransition(checker, currentState, toNode);
            Fsm.AddTransition(transition);
            currentState = toNode;
            return this;
        }

        public FSMLexerBuilder<TNode> RepetitionTransitionTo(string toNodeMark, int count, string pattern,
            TransitionPrecondition precondition = null)
        {
            var toNode = marks[toNodeMark];
            return RepetitionTransitionTo(toNode, count,pattern,precondition);
        }

        public FSMLexerBuilder<TNode> RepetitionTransitionTo(int toNode, int count, string pattern,
            TransitionPrecondition precondition = null)
        {
            var parsedPattern = ParseRepeatedPattern(pattern);

            if (count > 0 && !string.IsNullOrEmpty(pattern))
            {
                if (parsedPattern.ranges != null && parsedPattern.ranges.Any())
                {
                    for (int i = 0; i < count-1; i++)
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



        public FSMLexerBuilder<TNode> RangeTransitionTo(char start, char end, int toNode)
        {
            AbstractTransitionCheck checker = new TransitionRange(start, end);
            if (!Fsm.HasState(toNode)) Fsm.AddNode();
            var transition = new FSMTransition(checker, currentState, toNode);
            Fsm.AddTransition(transition);
            currentState = toNode;
            return this;
        }

        public FSMLexerBuilder<TNode> RangeTransitionTo(char start, char end, int toNode,
            TransitionPrecondition precondition)
        {
            AbstractTransitionCheck checker = new TransitionRange(start, end, precondition);
            if (!Fsm.HasState(toNode)) Fsm.AddNode();
            var transition = new FSMTransition(checker, currentState, toNode);
            Fsm.AddTransition(transition);
            currentState = toNode;
            return this;
        }

        #region multi range directed

        public FSMLexerBuilder<TNode> MultiRangeTransitionTo(int toNode, params (char start, char end)[] ranges)
        {
            AbstractTransitionCheck checker = new TransitionMultiRange(ranges);
            if (!Fsm.HasState(toNode)) Fsm.AddNode();
            var transition = new FSMTransition(checker, currentState, toNode);
            Fsm.AddTransition(transition);
            currentState = toNode;
            return this;
        }

        public FSMLexerBuilder<TNode> MultiRangeTransitionTo(int toNode,
            TransitionPrecondition precondition, params (char start, char end)[] ranges)
        {
            AbstractTransitionCheck checker = new TransitionMultiRange(precondition, ranges);
            if (!Fsm.HasState(toNode)) Fsm.AddNode();
            var transition = new FSMTransition(checker, currentState, toNode);
            Fsm.AddTransition(transition);
            currentState = toNode;
            return this;
        }

        public FSMLexerBuilder<TNode> MultiRangeTransitionTo(string toNodeMark, params (char start, char end)[] ranges)
        {
            var toNode = marks[toNodeMark];
            return MultiRangeTransitionTo(toNode,ranges);
        }


        #endregion


        public FSMLexerBuilder<TNode> ExceptTransitionTo(char[] exceptions, int toNode)
        {
            AbstractTransitionCheck checker = new TransitionAnyExcept(exceptions);
            if (!Fsm.HasState(toNode)) Fsm.AddNode();
            var transition = new FSMTransition(checker, currentState, toNode);
            Fsm.AddTransition(transition);
            currentState = toNode;
            return this;
        }

        public FSMLexerBuilder<TNode> ExceptTransitionTo(char[] exceptions, int toNode, TransitionPrecondition precondition)
        {
            AbstractTransitionCheck checker = new TransitionAnyExcept(precondition, exceptions);
            if (!Fsm.HasState(toNode)) Fsm.AddNode();
            var transition = new FSMTransition(checker, currentState, toNode);
            Fsm.AddTransition(transition);
            currentState = toNode;
            return this;
        }

        public FSMLexerBuilder<TNode> AnyTransitionTo(char input, int toNode)
        {
            AbstractTransitionCheck checker = new TransitionAny(input);
            if (!Fsm.HasState(toNode)) Fsm.AddNode();
            var transition = new FSMTransition(checker, currentState, toNode);
            Fsm.AddTransition(transition);
            currentState = toNode;
            return this;
        }

        public FSMLexerBuilder<TNode> AnyTransitionTo(char input, int toNode, TransitionPrecondition precondition)
        {
            AbstractTransitionCheck checker = new TransitionAny(input, precondition);
            if (!Fsm.HasState(toNode)) Fsm.AddNode();
            var transition = new FSMTransition(checker, currentState, toNode);
            Fsm.AddTransition(transition);
            currentState = toNode;
            return this;
        }

        public FSMLexerBuilder<TNode> TransitionTo(char input, string toNodeMark)
        {
            var toNode = marks[toNodeMark];
            return TransitionTo(input, toNode);
        }


        public FSMLexerBuilder<TNode> TransitionTo(char input, string toNodeMark, TransitionPrecondition precondition)
        {
            var toNode = marks[toNodeMark];
            return TransitionTo(input, toNode, precondition);
        }

        public FSMLexerBuilder<TNode> RangeTransitionTo(char start, char end, string toNodeMark)
        {
            var toNode = marks[toNodeMark];
            return RangeTransitionTo(start, end, toNode);
        }

        public FSMLexerBuilder<TNode> RangeTransitionTo(char start, char end, string toNodeMark,
            TransitionPrecondition precondition)
        {
            var toNode = marks[toNodeMark];
            return RangeTransitionTo(start, end, toNode, precondition);
        }

        public FSMLexerBuilder<TNode> ExceptTransitionTo(char[] exceptions, string toNodeMark)
        {
            var toNode = marks[toNodeMark];
            return ExceptTransitionTo(exceptions, toNode);
        }

        public FSMLexerBuilder<TNode> ExceptTransitionTo(char[] exceptions, string toNodeMark,
            TransitionPrecondition precondition)
        {
            var toNode = marks[toNodeMark];
            return ExceptTransitionTo(exceptions, toNode, precondition);
        }

        public FSMLexerBuilder<TNode> AnyTransitionTo(char input, string toNodeMark)
        {
            var toNode = marks[toNodeMark];
            return AnyTransitionTo(input, toNode);
        }

        public FSMLexerBuilder<TNode> AnyTransitionTo(char input, string toNodeMark, TransitionPrecondition precondition)
        {
            var toNode = marks[toNodeMark];
            return AnyTransitionTo(input, toNode, precondition);
        }

        #endregion
    }

    
}