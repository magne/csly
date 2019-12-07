using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using sly.parser.syntax.grammar;

namespace sly.parser.generator
{
    public class NonTerminal<TIn> where TIn : struct
    {
        public NonTerminal(string name, List<Rule<TIn>> rules)
        {
            Name = name;
            Rules = rules;
        }

        public NonTerminal(string name) : this(name, new List<Rule<TIn>>())
        { }

        public string Name { get; set; }

        public List<Rule<TIn>> Rules { get; set; }

        public bool IsSubRule { get; set; }

        public List<TIn> PossibleLeadingTokens => Rules.SelectMany(r => r.PossibleLeadingTokens).ToList();

        [ExcludeFromCodeCoverage]
        public string Dump()
        {
            StringBuilder dump = new StringBuilder();

            foreach (var rule in Rules)
            {
                dump.Append(Name).Append(" : ");
                foreach (IClause<TIn> clause in rule.Clauses)
                {
                    dump.Append(clause.Dump()).Append(" ");
                }

                dump.AppendLine();
            }

            return dump.ToString();
        }
    }
}