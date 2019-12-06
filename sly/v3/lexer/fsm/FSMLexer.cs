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

    internal delegate void BuildExtension<IN>(IN token, LexemeAttribute lexem, GenericLexer<IN> lexer) where IN : struct;

    internal class FSMLexer<N>
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
            var tokenStartIndex = start;
            var result = new FSMMatch<N>(false);
            var successes = new Stack<FSMMatch<N>>();
            CurrentPosition = start;
            var currentNode = Nodes[0];
            TokenPosition position = null;

            var tokenStarted = false;


            if (CurrentPosition < source.Length)
            {
                while (CurrentPosition < source.Length && currentNode != null)
                {
                    var currentCharacter = source.Span[CurrentPosition];

                    var consumeSkipped = true;

                    while (consumeSkipped && !tokenStarted && CurrentPosition < source.Length)
                    {
                        currentCharacter = source.At(CurrentPosition);
                        if (IgnoreWhiteSpace && WhiteSpaces.Contains(currentCharacter))
                        {
                            if (successes.Any())
                                currentNode = null;
                            else
                                currentNode = Nodes[0];
                            CurrentPosition++;
                            CurrentColumn++;
                        }
                        else
                        {
                            var eol = EOLManager.IsEndOfLine(source, CurrentPosition);

                            if (IgnoreEOL && eol != EOLType.No)
                            {
                                if (successes.Any())
                                    currentNode = null;
                                else
                                    currentNode = Nodes[0];
                                CurrentPosition += eol == EOLType.Windows ? 2 : 1;
                                CurrentColumn = 0;
                                CurrentLine++;
                            }
                            else
                            {
                                consumeSkipped = false;
                            }
                        }
                        tokenStartIndex = CurrentPosition;
                    }

                    if (CurrentPosition >= source.Length)
                    {
                        return new FSMMatch<N>(false);
                    }
                    var currentValue = source.Slice(tokenStartIndex, CurrentPosition - tokenStartIndex + 1);


                    currentNode = Move(currentNode, currentCharacter, currentValue);
                    if (currentNode != null)
                    {
                        if (!tokenStarted)
                        {
                            tokenStarted = true;
                            position = new TokenPosition(CurrentPosition, CurrentLine, CurrentColumn);
                        }

                        if (currentNode.IsEnd)
                        {
                            var resultInter = new FSMMatch<N>(true, currentNode.Value, currentValue, position, currentNode.Id);
                            successes.Push(resultInter);
                        }

                        CurrentPosition++;
                        CurrentColumn++;
                    }
                    else
                    {
                        if (!successes.Any() && CurrentPosition < source.Length)
                        {
                            var errorChar = source.Slice(CurrentPosition, 1);
                            var errorPosition = new TokenPosition(CurrentPosition, CurrentLine, CurrentColumn);
                            var ko = new FSMMatch<N>(false, default(N), errorChar, errorPosition, -1);
                            return ko;
                        }
                    }
                }
            }


            if (successes.Any())
            {
                result = successes.Pop();
                var node = Nodes[result.NodeId];
                if (node.HasCallback)
                {
                    result = node.Callback(result);
                }
            }

            return result;
        }

        private FSMNode<N> Move(FSMNode<N> from, char token, ReadOnlyMemory<char> value)
        {
            FSMNode<N> next = null;
            if (from != null)
            {
                if (Transitions[from.Id] != null)
                {
                    var transitions = Transitions[from.Id];
                    if (transitions.Any())
                    {
                        var i = 0;
                        var transition = transitions[i];
                        var match = transition.Match(token, value);

                        while (i < transitions.Count && !match)
                        {
                            transition = transitions[i];
                            match = transition.Match(token, value);
                            i++;
                        }

                        if (match)
                        {
                            next = Nodes[transition.ToNode];
                        }
                    }
                }
            }

            return next;
        }

        #endregion
    }
}