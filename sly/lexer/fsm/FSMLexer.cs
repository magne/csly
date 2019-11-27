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

    public delegate void BuildExtension<IN>(IN token, LexemeAttribute lexem, GenericLexer<IN> lexer) where IN : struct;

    public class FSMLexer<N>
    {
        private readonly List<FSMNode<N>> Nodes;

        private readonly List<List<FSMTransition>> Transitions;

        public FSMLexer(List<FSMNode<N>> nodes, List<List<FSMTransition>> transitions)
        {
            Nodes = nodes;
            Transitions = transitions;
            IgnoreWhiteSpace = false;
            IgnoreEOL = false;
            AggregateEOL = false;
            WhiteSpaces = new List<char>();
        }

        public bool IgnoreWhiteSpace { get; set; }

        public List<char> WhiteSpaces { get; }

        public bool IgnoreEOL { get; set; }

        public bool AggregateEOL { get; set; }

        [ExcludeFromCodeCoverage]
        public string ToGraphViz()
        {
            var dump = new StringBuilder();
            foreach (var transitions in Transitions.Where(t => t != null))
                foreach (var transition in transitions)
                    dump.AppendLine(transition.ToGraphViz(Nodes));
            return dump.ToString();
        }

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

        public FSMMatch<N> Run(string source)
        {
            return Run(source, CurrentPosition);
        }

        public FSMMatch<N> Run(ReadOnlyMemory<char> source)
        {
            return Run(source, CurrentPosition);
        }

        public FSMMatch<N> Run(string source, int start)
        {
            return Run(new ReadOnlyMemory<char>(source.ToCharArray()), start);
        }

        public FSMMatch<N> Run(ReadOnlyMemory<char> source, int start)
        {
            CurrentPosition = start;
            ConsumeIgnored(source);

            // End of token stream
            if (CurrentPosition >= source.Length)
            {
                return new FSMMatch<N>(false);
            }

            // Make a note of where current token starts
            var position = new TokenPosition(CurrentPosition, CurrentLine, CurrentColumn);

            FSMMatch<N> result = null;
            var currentNode = Nodes[0];
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
                    result = new FSMMatch<N>(true, currentNode.Value, currentValue, position, currentNode.Id);
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

                var node = Nodes[result.NodeId];
                if (node.HasCallback)
                {
                    result = node.Callback(result);
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
            var ko = new FSMMatch<N>(false, default(N), errorChar, errorPosition, -1);
            return ko;
        }

        private FSMNode<N> Move(FSMNode<N> from, char token, ReadOnlyMemory<char> value)
        {
            if (from != null && Transitions[from.Id] != null)
            {
                // Do NOT use Linq, increases allocations AND running time
                var transitions = Transitions[from.Id];
                for (var i = 0; i < transitions.Count; ++i)
                {
                    var transition = transitions[i];
                    if (transition.Match(token, value))
                    {
                        return Nodes[transition.ToNode];
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
    }
}