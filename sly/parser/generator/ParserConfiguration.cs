using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace sly.parser.generator
{
    // ReSharper disable once UnusedTypeParameter
    public class ParserConfiguration<TIn, TOut> where TIn : struct
    {
        public string StartingRule { get; set; }
        public Dictionary<string, NonTerminal<TIn>> NonTerminals { get; set; }


        public void AddNonTerminalIfNotExists(NonTerminal<TIn> nonTerminal)
        {
            if (!NonTerminals.ContainsKey(nonTerminal.Name)) NonTerminals[nonTerminal.Name] = nonTerminal;
        }

        [ExcludeFromCodeCoverage]
        public string Dump()
        {
            StringBuilder dump = new StringBuilder();
            foreach (NonTerminal<TIn> nonTerminal in NonTerminals.Values)
            {
                dump.AppendLine(nonTerminal.Dump());
            }

            return dump.ToString();
        }
    }
}