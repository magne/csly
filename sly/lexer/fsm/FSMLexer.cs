using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace sly.lexer.fsm
{
    public static class MemoryExtensions
    {
        public static T At<T>(this ReadOnlyMemory<T> memory, int index)
        {
            return memory.Span[index];
        }
    }

    public delegate void BuildExtension<TLexeme>(TLexeme token, LexemeAttribute lexem, GenericLexer<TLexeme> lexer) where TLexeme : struct;

    // ReSharper disable once InconsistentNaming
    public class FSMLexer<TNode>
    {
        private readonly Dictionary<int, FSMNode<TNode>> nodes;

        public char StringDelimiter = '"';

        private readonly Dictionary<int, List<FSMTransition>> transitions;

        public FSMLexer()
        {
            nodes = new Dictionary<int, FSMNode<TNode>>();
            transitions = new Dictionary<int, List<FSMTransition>>();
            Callbacks = new Dictionary<int, NodeCallback<TNode>>();
            IgnoreWhiteSpace = false;
            IgnoreEOL = false;
            AggregateEOL = false;
            WhiteSpaces = new List<char>();
        }

        public bool IgnoreWhiteSpace { get; set; }

        public List<char> WhiteSpaces { get; set; }

        // ReSharper disable once InconsistentNaming
        public bool IgnoreEOL { get; set; }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool AggregateEOL { get; set; }


        private Dictionary<int, NodeCallback<TNode>> Callbacks { get; }

        [ExcludeFromCodeCoverage]
        public string ToGraphViz()
        {
            var dump = new StringBuilder();
            foreach (var fsmTransitions in transitions.Values)
                foreach (var transition in fsmTransitions)
                    dump.AppendLine(transition.ToGraphViz(nodes));
            return dump.ToString();
        }

        #region accessors

        internal bool HasState(int state)
        {
            return nodes.ContainsKey(state);
        }

        internal FSMNode<TNode> GetNode(int state)
        {
            nodes.TryGetValue(state, out var node);
            return node;
        }

        internal int NewNodeId => nodes.Count;

        internal bool HasCallback(int nodeId)
        {
            return Callbacks.ContainsKey(nodeId);
        }

        internal void SetCallback(int nodeId, NodeCallback<TNode> callback)
        {
            Callbacks[nodeId] = callback;
        }

        #endregion

        #region build

        public FSMTransition GetTransition(int nodeId, char token)
        {
            FSMTransition transition = null;
            if (HasState(nodeId))
                if (transitions.ContainsKey(nodeId))
                {
                    var leavingTransitions = transitions[nodeId];
                    transition = leavingTransitions.FirstOrDefault(t => t.Match(token));
                }

            return transition;
        }


        public void AddTransition(FSMTransition transition)
        {
            var fsmTransitions = new List<FSMTransition>();
            if (this.transitions.ContainsKey(transition.FromNode)) fsmTransitions = this.transitions[transition.FromNode];
            fsmTransitions.Add(transition);
            this.transitions[transition.FromNode] = fsmTransitions;
        }


        public FSMNode<TNode> AddNode(TNode value)
        {
            var node = new FSMNode<TNode>(value);
            node.Id = nodes.Count;
            nodes[node.Id] = node;
            return node;
        }

        public FSMNode<TNode> AddNode()
        {
            var node = new FSMNode<TNode>(default(TNode));
            node.Id = nodes.Count;
            nodes[node.Id] = node;
            return node;
        }

        #endregion

        #region run

        public int CurrentPosition { get; private set; }
        public int CurrentColumn { get; private set; }
        public int CurrentLine { get; private set; }

        public void MovePosition(int newPosition, int newLine, int newColumn)
        {
            CurrentPosition = newPosition;
            CurrentLine = newLine;
            CurrentColumn = newColumn;
        }

        public FSMMatch<TNode> Run(string source)
        {
            return Run(source, CurrentPosition);
        }

        public FSMMatch<TNode> Run(ReadOnlyMemory<char> source)
        {
            return Run(source, CurrentPosition);
        }

        public FSMMatch<TNode> Run(string source, int start)
        {
            return Run(new ReadOnlyMemory<char>(source.ToCharArray()), start);
        }

        public FSMMatch<TNode> Run(ReadOnlyMemory<char> source, int start)
        {
            CurrentPosition = start;
            ConsumeIgnored(source);

            // End of token stream
            if (CurrentPosition >= source.Length)
            {
                return new FSMMatch<TNode>(false);
            }

            // Make a note of where current token starts
            var position = new TokenPosition(CurrentPosition, CurrentLine, CurrentColumn);

            FSMMatch<TNode> result = null;
            var currentNode = nodes[0];
            while (CurrentPosition < source.Length)
            {
                var currentCharacter = source.At(CurrentPosition);
                var currentValue = source.Slice(position.Index, CurrentPosition - position.Index + 1);
                currentNode = Move(currentNode, currentCharacter, currentValue);
                if (currentNode == null)
                {
                    // No more viable transitions, so exit loop
                    break;
                }

                if (currentNode.IsEnd)
                {
                    // Remember the possible match
                    result = new FSMMatch<TNode>(true, currentNode.Value, currentValue, position, currentNode.Id);
                }

                CurrentPosition++;
                CurrentColumn++;
            }

            if (result != null)
            {
                // Backtrack
                var length = result.Result.Value.Length;
                CurrentPosition = result.Result.Position.Index + length;
                CurrentColumn = result.Result.Position.Column + length;

                if (HasCallback(result.NodeId))
                {
                    result = Callbacks[result.NodeId](result);
                }

                return result;
            }

            if (CurrentPosition >= source.Length)
            {
                // Failed on last character, so need to backtrack
                CurrentPosition -= 1;
                CurrentColumn -= 1;
            }

            var errorChar = source.Slice(CurrentPosition, 1);
            var errorPosition = new TokenPosition(CurrentPosition, CurrentLine, CurrentColumn);
            var ko = new FSMMatch<TNode>(false, default(TNode), errorChar, errorPosition, -1);
            return ko;
        }

        private FSMNode<TNode> Move(FSMNode<TNode> from, char token, ReadOnlyMemory<char> value)
        {
            if (from != null && this.transitions.TryGetValue(from.Id, out var fsmTransitions))
            {
                // Do NOT use Linq, increases allocations AND running time
                for (var i = 0; i < fsmTransitions.Count; ++i)
                {
                    var transition = fsmTransitions[i];
                    if (transition.Match(token, value))
                    {
                        return nodes[transition.ToNode];
                    }
                }
            }

            return null;
        }

        private void ConsumeIgnored(ReadOnlyMemory<char> source)
        {
            while (CurrentPosition < source.Length)
            {
                if (IgnoreWhiteSpace)
                {
                    var currentCharacter = source.At(CurrentPosition);
                    if (WhiteSpaces.Contains(currentCharacter))
                    {
                        CurrentPosition++;
                        CurrentColumn++;
                        continue;
                    }
                }

                if (IgnoreEOL)
                {
                    var eol = EOLManager.IsEndOfLine(source, CurrentPosition);
                    if (eol != EOLType.No)
                    {
                        CurrentPosition += eol == EOLType.Windows ? 2 : 1;
                        CurrentColumn = 0;
                        CurrentLine++;
                        continue;
                    }
                }

                break;
            }
        }

        #endregion
        
        public void Reset()
        {
            CurrentColumn = 0;
            CurrentLine = 0;
            CurrentPosition = 0;
        }
    }
}