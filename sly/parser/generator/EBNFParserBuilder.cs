using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using sly.buildresult;
using sly.lexer.fsm;
using sly.parser.generator.visitor;
using sly.parser.llparser;
using sly.parser.syntax.grammar;

namespace sly.parser.generator
{
    /// <summary>
    ///     this class provides API to build parser
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal class EBNFParserBuilder<TIn, TOut> : ParserBuilder<TIn, TOut> where TIn : struct
    {
        public override BuildResult<Parser<TIn, TOut>> BuildParser(object parserInstance, ParserType parserType,
            string rootRule, BuildExtension<TIn> extensionBuilder = null)
        {
            var ruleparser = new RuleParser<TIn>();
            var builder = new ParserBuilder<EbnfTokenGeneric, GrammarNode<TIn>>();

            var grammarParser = builder.BuildParser(ruleparser, ParserType.LL_RECURSIVE_DESCENT, "rule").Result;


            var result = new BuildResult<Parser<TIn, TOut>>();

            ParserConfiguration<TIn, TOut> configuration;

            try
            {
                configuration = ExtractEbnfParserConfiguration(parserInstance.GetType(), grammarParser);
                configuration.StartingRule = rootRule;
            }
            catch (Exception e)
            {
                result.AddError(new ParserInitializationError(ErrorLevel.ERROR, e.Message));
                return result;
            }

            var syntaxParser = BuildSyntaxParser(configuration, parserType, rootRule);

            SyntaxTreeVisitor<TIn, TOut> visitor = null;
            if (parserType == ParserType.LL_RECURSIVE_DESCENT)
            {
                visitor = new SyntaxTreeVisitor<TIn, TOut>(configuration, parserInstance);
            }
            else if (parserType == ParserType.EBNF_LL_RECURSIVE_DESCENT)
            {
                visitor = new EBNFSyntaxTreeVisitor<TIn, TOut>(configuration, parserInstance);
            }
            var parser = new Parser<TIn, TOut>(syntaxParser, visitor);
            parser.Configuration = configuration;
            var lexerResult = BuildLexer();
            if (lexerResult.IsError)
                result.AddErrors(lexerResult.Errors);
            else
                parser.Lexer = lexerResult.Result;
            parser.Instance = parserInstance;
            result.Result = parser;
            return result;
        }


        protected override ISyntaxParser<TIn, TOut> BuildSyntaxParser(ParserConfiguration<TIn, TOut> conf,
            ParserType parserType,
            string rootRule)
        {
            ISyntaxParser<TIn, TOut> parser;
            switch (parserType)
            {
                case ParserType.LL_RECURSIVE_DESCENT:
                {
                    parser = new RecursiveDescentSyntaxParser<TIn, TOut>(conf, rootRule);
                    break;
                }
                case ParserType.EBNF_LL_RECURSIVE_DESCENT:
                {
                    parser = new EBNFRecursiveDescentSyntaxParser<TIn, TOut>(conf, rootRule);
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


        #region configuration

        protected virtual ParserConfiguration<TIn, TOut> ExtractEbnfParserConfiguration(Type parserClass,
            Parser<EbnfTokenGeneric, GrammarNode<TIn>> grammarParser)
        {
            var conf = new ParserConfiguration<TIn, TOut>();
            var nonTerminals = new Dictionary<string, NonTerminal<TIn>>();
            var methods = parserClass.GetMethods().ToList();
            methods = methods.Where(m =>
            {
                var attributes = m.GetCustomAttributes().ToList();
                var attr = attributes.Find(a => a.GetType() == typeof(ProductionAttribute));
                return attr != null;
            }).ToList();

            methods.ForEach(m =>
            {
                var attributes =
                    (ProductionAttribute[]) m.GetCustomAttributes(typeof(ProductionAttribute), true);

                foreach (var attr in attributes)
                {
                    var ruleString = attr.RuleString;
                    var parseResult = grammarParser.Parse(ruleString);
                    if (!parseResult.IsError)
                    {
                        var rule = (Rule<TIn>) parseResult.Result;
                        rule.RuleString = ruleString;
                        rule.SetVisitor(m);
                        NonTerminal<TIn> nonT;
                        if (!nonTerminals.ContainsKey(rule.NonTerminalName))
                            nonT = new NonTerminal<TIn>(rule.NonTerminalName, new List<Rule<TIn>>());
                        else
                            nonT = nonTerminals[rule.NonTerminalName];
                        nonT.Rules.Add(rule);
                        nonTerminals[rule.NonTerminalName] = nonT;
                    }
                    else
                    {
                        var message = parseResult
                            .Errors
                            .Select(e => e.ErrorMessage)
                            .Aggregate((e1, e2) => e1 + "\n" + e2);
                        message = $"rule error [{ruleString}] : {message}";
                        throw new ParserConfigurationException(message);
                    }
                }
            });

            conf.NonTerminals = nonTerminals;

            return conf;
        }

        #endregion
    }
}