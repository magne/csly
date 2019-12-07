using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace sly.v3.lexer.fsm
{

    internal static class MemoryExtensions
    {

        public static T At<T>(this ReadOnlyMemory<T> memory, int index)
        {
            return memory.Span[index];
        }
    }

    internal delegate void BuildExtension<TIn>(TIn token, LexemeAttribute lexem, GenericLexer<TIn> lexer) where TIn : struct;

    // ReSharper disable once InconsistentNaming
    internal class FSMLexer<T>
    {
        private readonly List<FSMNode<T>> nodes;

        private readonly List<List<FSMTransition>> transitions;

        public FSMLexer(List<FSMNode<T>> nodes, List<List<FSMTransition>> transitions)
        {
            this.nodes = nodes;
            this.transitions = transitions;
            IgnoreWhiteSpace = false;
            IgnoreEOL = false;
            WhiteSpaces = new List<char>();
        }

        public bool IgnoreWhiteSpace { get; set; }

        public List<char> WhiteSpaces { get; }

        // ReSharper disable once InconsistentNaming
        public bool IgnoreEOL { get; set; }

        [ExcludeFromCodeCoverage]
        public string ToGraphViz()
        {
            var dump = new StringBuilder();
            foreach (var fsmTransitions in transitions.Where(t => t != null))
                foreach (var transition in fsmTransitions)
                    dump.AppendLine(transition.ToGraphViz(nodes));
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

        public FSMMatch<T> Run(string source)
        {
            return Run(source, CurrentPosition);
        }

        public FSMMatch<T> Run(ReadOnlyMemory<char> source)
        {
            return Run(source, CurrentPosition);
        }

        public FSMMatch<T> Run(string source, int start)
        {
            return Run(new ReadOnlyMemory<char>(source.ToCharArray()), start);
        }

        public FSMMatch<T> Run(ReadOnlyMemory<char> source, int start)
        {
            CurrentPosition = start;
            ConsumeIgnored(source);

            // End of token stream
            if (CurrentPosition >= source.Length)
            {
                return new FSMMatch<T>(false);
            }

            // Make a note of where current token starts
            var position = new TokenPosition(CurrentPosition, CurrentLine, CurrentColumn);

            FSMMatch<T> result = null;
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
                    result = new FSMMatch<T>(true, currentNode.Value, currentValue, position, currentNode.Id);
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

                var node = nodes[result.NodeId];
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
            var ko = new FSMMatch<T>(false, default(T), errorChar, errorPosition, -1);
            return ko;
        }

        private FSMNode<T> Move(FSMNode<T> from, char token, ReadOnlyMemory<char> value)
        {
            FSMNode<T> next = null;
            if (from != null)
            {
                if (transitions[from.Id] != null)
                {
                    var fsmTransitions = this.transitions[from.Id];
                    if (fsmTransitions.Any())
                    {
                        var i = 0;
                        var transition = fsmTransitions[i];
                        var match = transition.Match(token, value);

                        while (i < fsmTransitions.Count && !match)
                        {
                            transition = fsmTransitions[i];
                            match = transition.Match(token, value);
                            i++;
                        }

                        if (match)
                        {
                            next = nodes[transition.ToNode];
                        }
                    }
                }
            }

            return next;
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