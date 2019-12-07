using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using sly.buildresult;
using sly.lexer;
using sly.parser.generator.visitor;
using sly.parser.llparser;
using sly.parser.syntax.grammar;

namespace sly.parser.generator
{
    public delegate BuildResult<Parser<TIn, TOut>> ParserChecker<TIn, TOut>(BuildResult<Parser<TIn, TOut>> result,
        NonTerminal<TIn> nonterminal) where TIn : struct;

    /// <summary>
    ///     this class provides API to build parser
    /// </summary>
    public class ParserBuilder<TIn, TOut> where TIn : struct
    {
        #region API

        /// <summary>
        ///     Builds a parser (lexer, syntax parser and syntax tree visitor) according to a parser definition instance
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <param name="parserInstance">
        ///     a parser definition instance , containing
        ///     [Reduction] methods for grammar rules
        ///     <param name="parserType">
        ///         a ParserType enum value stating the analyser type (LR, LL ...) for now only LL recurive
        ///         descent parser available
        ///     </param>
        ///     <param name="rootRule">the name of the root non terminal of the grammar</param>
        ///     <returns></returns>
        public virtual BuildResult<Parser<TIn, TOut>> BuildParser(object parserInstance, ParserType parserType,
            string rootRule)
        {
            Parser<TIn, TOut> parser;
            var result = new BuildResult<Parser<TIn, TOut>>();
            if (parserType == ParserType.LL_RECURSIVE_DESCENT)
            {
                var configuration = ExtractParserConfiguration(parserInstance.GetType());
                configuration.StartingRule = rootRule;
                var syntaxParser = BuildSyntaxParser(configuration, parserType, rootRule);
                var visitor = new SyntaxTreeVisitor<TIn, TOut>(configuration, parserInstance);
                parser = new Parser<TIn, TOut>(syntaxParser, visitor);
                var lexerResult = BuildLexer();
                parser.Lexer = lexerResult.Result;
                if (lexerResult.IsError) result.AddErrors(lexerResult.Errors);
                parser.Instance = parserInstance;
                parser.Configuration = configuration;
                result.Result = parser;
            }
            else if (parserType == ParserType.EBNF_LL_RECURSIVE_DESCENT)
            {
                var builder = new EBNFParserBuilder<TIn, TOut>();
                result = builder.BuildParser(parserInstance, ParserType.EBNF_LL_RECURSIVE_DESCENT, rootRule);
            }

            parser = result.Result;
            if (!result.IsError)
            {
                var expressionResult = parser.BuildExpressionParser(result, rootRule);
                if (expressionResult.IsError) result.AddErrors(expressionResult.Errors);
                result.Result.Configuration = expressionResult.Result;

                result = CheckParser(result);
            }

            return result;
        }


        protected virtual ISyntaxParser<TIn, TOut> BuildSyntaxParser(ParserConfiguration<TIn, TOut> conf,
            ParserType parserType, string rootRule)
        {
            ISyntaxParser<TIn, TOut> parser;
            switch (parserType)
            {
                case ParserType.LL_RECURSIVE_DESCENT:
                {
                    parser = new RecursiveDescentSyntaxParser<TIn, TOut>(conf, rootRule);
                    break;
                }
                default:
                {
                    parser = null;
                    break;
                }
            }

            return parser;
        }

        #endregion


        #region CONFIGURATION

        private Tuple<string, string> ExtractNTAndRule(string ruleString)
        {
            Tuple<string, string> result = null;
            if (ruleString != null)
            {
                string nt;
                string rule;
                var i = ruleString.IndexOf(":", StringComparison.Ordinal);
                if (i > 0)
                {
                    nt = ruleString.Substring(0, i).Trim();
                    rule = ruleString.Substring(i + 1);
                    result = new Tuple<string, string>(nt, rule);
                }
            }

            return result;
        }


        protected virtual BuildResult<ILexer<TIn>> BuildLexer()
        {
            var lexer = LexerBuilder.BuildLexer(new BuildResult<ILexer<TIn>>());
            return lexer;
        }


        protected virtual ParserConfiguration<TIn, TOut> ExtractParserConfiguration(Type parserClass)
        {
            var conf = new ParserConfiguration<TIn, TOut>();
            var functions = new Dictionary<string, MethodInfo>();
            var nonTerminals = new Dictionary<string, NonTerminal<TIn>>();
            var methods = parserClass.GetMethods().ToList();
            methods = methods.Where(m =>
            {
                var attributes = m.GetCustomAttributes().ToList();
                var attr = attributes.Find(a => a.GetType() == typeof(ProductionAttribute));
                return attr != null;
            }).ToList();

            parserClass.GetMethods();
            methods.ForEach(m =>
            {
                var attributes = (ProductionAttribute[]) m.GetCustomAttributes(typeof(ProductionAttribute), true);

                foreach (var attr in attributes)
                {
                    var ntAndRule = ExtractNTAndRule(attr.RuleString);


                    var r = BuildNonTerminal(ntAndRule);
                    r.SetVisitor(m);
                    r.NonTerminalName = ntAndRule.Item1;
                    var key = ntAndRule.Item1 + "__" + r.Key;
                    functions[key] = m;
                    NonTerminal<TIn> nonT;
                    if (!nonTerminals.ContainsKey(ntAndRule.Item1))
                        nonT = new NonTerminal<TIn>(ntAndRule.Item1, new List<Rule<TIn>>());
                    else
                        nonT = nonTerminals[ntAndRule.Item1];
                    nonT.Rules.Add(r);
                    nonTerminals[ntAndRule.Item1] = nonT;
                }
            });

            conf.NonTerminals = nonTerminals;

            return conf;
        }

        private Rule<TIn> BuildNonTerminal(Tuple<string, string> ntAndRule)
        {
            var rule = new Rule<TIn>();

            var clauses = new List<IClause<TIn>>();
            var ruleString = ntAndRule.Item2;
            var clausesString = ruleString.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in clausesString)
            {
                IClause<TIn> clause = null;
                var isTerminal = false;
                var token = default(TIn);
                try
                {
                    var b = Enum.TryParse(item, out token);
                    if (b)
                    {
                        isTerminal = true;
                    }

                    //token = (IN)Enum.Parse(tIn , item);
                    //isTerminal = true;
                }
                catch (ArgumentException)
                {
                    isTerminal = false;
                }

                if (isTerminal)
                {
                    clause = new TerminalClause<TIn>(token);
                }
                else if (item == "[d]")
                {
                    if (clauses.Last() is TerminalClause<TIn> discardedTerminal) discardedTerminal.Discarded = true;
                }
                else
                {
                    clause = new NonTerminalClause<TIn>(item);
                }

                if (clause != null) clauses.Add(clause);
            }

            rule.Clauses = clauses;
            //rule.Key = ntAndRule.Item1 + "_" + ntAndRule.Item2.Replace(" ", "_");

            return rule;
        }

        #endregion

        #region parser checking

        private BuildResult<Parser<TIn, TOut>> CheckParser(BuildResult<Parser<TIn, TOut>> result)
        {
            var checkers = new List<ParserChecker<TIn, TOut>>();
            checkers.Add(CheckUnreachable);
            checkers.Add(CheckNotFound);

            if (result.Result != null && !result.IsError)
                foreach (var checker in checkers)
                    if (checker != null)
                        result.Result.Configuration.NonTerminals.Values.ToList()
                            .ForEach(nt => result = checker(result, nt));
            return result;
        }

        private static BuildResult<Parser<TIn, TOut>> CheckUnreachable(BuildResult<Parser<TIn, TOut>> result,
            NonTerminal<TIn> nonTerminal)
        {
            var conf = result.Result.Configuration;
            var found = false;
            if (nonTerminal.Name != conf.StartingRule)
            {
                foreach (var nt in result.Result.Configuration.NonTerminals.Values.ToList())
                    if (nt.Name != nonTerminal.Name)
                    {
                        found = NonTerminalReferences(nt, nonTerminal.Name);
                        if (found) break;
                    }

                if (!found)
                    result.AddError(new ParserInitializationError(ErrorLevel.WARN,
                        $"non terminal [{nonTerminal.Name}] is never used."));
            }

            return result;
        }


        private static bool NonTerminalReferences(NonTerminal<TIn> nonTerminal, string referenceName)
        {
            var found = false;
            var iRule = 0;
            while (iRule < nonTerminal.Rules.Count && !found)
            {
                var rule = nonTerminal.Rules[iRule];
                var iClause = 0;
                while (iClause < rule.Clauses.Count && !found)
                {
                    var clause = rule.Clauses[iClause];
                    if (clause is NonTerminalClause<TIn> ntClause)
                    {
                        if (ntClause != null) found = found || ntClause.NonTerminalName == referenceName;
                    }
                    else if (clause is OptionClause<TIn> option)
                    {
                        if (option != null && option.Clause is NonTerminalClause<TIn> inner)
                            found = found || inner.NonTerminalName == referenceName;
                    }
                    else if (clause is ZeroOrMoreClause<TIn> zeroOrMore)
                    {
                        if (zeroOrMore != null && zeroOrMore.Clause is NonTerminalClause<TIn> inner)
                            found = found || inner.NonTerminalName == referenceName;
                    }
                    else if (clause is OneOrMoreClause<TIn> oneOrMore)
                    {
                        if (oneOrMore != null && oneOrMore.Clause is NonTerminalClause<TIn> inner)
                            found = found || inner.NonTerminalName == referenceName;
                    }

                    iClause++;
                }

                iRule++;
            }

            return found;
        }


        private static BuildResult<Parser<TIn, TOut>> CheckNotFound(BuildResult<Parser<TIn, TOut>> result,
            NonTerminal<TIn> nonTerminal)
        {
            var conf = result.Result.Configuration;
            foreach (var rule in nonTerminal.Rules)
            foreach (var clause in rule.Clauses)
                if (clause is NonTerminalClause<TIn> ntClause)
                    if (!conf.NonTerminals.ContainsKey(ntClause.NonTerminalName))
                        result.AddError(new ParserInitializationError(ErrorLevel.ERROR,
                            $"{ntClause.NonTerminalName} references from {rule.RuleString} does not exist."));
            return result;
        }

        #endregion
    }
}