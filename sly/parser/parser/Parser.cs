using System.Collections.Generic;
using sly.buildresult;
using sly.lexer;
using sly.parser.generator;
using sly.parser.generator.visitor;
using sly.parser.parser;

namespace sly.parser
{
    public class Parser<TIn, TOut> where TIn : struct
    {
        public Parser(ISyntaxParser<TIn, TOut> syntaxParser, SyntaxTreeVisitor<TIn, TOut> visitor)
        {
            SyntaxParser = syntaxParser;
            Visitor = visitor;
        }

        public ILexer<TIn> Lexer { get; set; }
        public object Instance { get; set; }
        public ISyntaxParser<TIn, TOut> SyntaxParser { get; set; }
        public SyntaxTreeVisitor<TIn, TOut> Visitor { get; set; }
        public ParserConfiguration<TIn, TOut> Configuration { get; set; }


        #region expression generator

        public virtual BuildResult<ParserConfiguration<TIn, TOut>> BuildExpressionParser(
            BuildResult<Parser<TIn, TOut>> result, string startingRule = null)
        {
            var exprResult = new BuildResult<ParserConfiguration<TIn, TOut>>(Configuration);
            exprResult = ExpressionRulesGenerator.BuildExpressionRules(Configuration, Instance.GetType(), exprResult);
            Configuration = exprResult.Result;
            SyntaxParser.Init(exprResult.Result, startingRule);
            if (startingRule != null)
            {
                Configuration.StartingRule = startingRule;
                SyntaxParser.StartingNonTerminal = startingRule;
            }

            if (exprResult.IsError)
                result.AddErrors(exprResult.Errors);
            else
                result.Result.Configuration = Configuration;
            return exprResult;
        }

        #endregion


        public ParseResult<TIn, TOut> Parse(string source, string startingNonTerminal = null)
        {
            return ParseWithContext(source, new NoContext(), startingNonTerminal);
        }


        public ParseResult<TIn, TOut> ParseWithContext(string source, object context, string startingNonTerminal = null)
        {
            ParseResult<TIn, TOut> result = null;
            Lexer.ResetLexer();
            var lexingResult = Lexer.Tokenize(source);
            if (lexingResult.IsError)
            {
                result = new ParseResult<TIn, TOut>();
                result.IsError = true;
                result.Errors = new List<ParseError>();
                result.Errors.Add(lexingResult.Error);
                return result;
            }

            var tokens = lexingResult.Tokens;
            var position = 0;
            var tokensWithoutComments = new List<Token<TIn>>();
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (!token.IsComment)
                {
                    token.PositionInTokenFlow = position;
                    tokensWithoutComments.Add(token);
                    position++;
                }
            }

            result = ParseWithContext(tokensWithoutComments, context, startingNonTerminal);


            return result;
        }

        public ParseResult<TIn, TOut> ParseWithContext(IList<Token<TIn>> tokens, object parsingContext = null, string startingNonTerminal = null)
        {
            var result = new ParseResult<TIn, TOut>();

            var cleaner = new SyntaxTreeCleaner<TIn>();
            var syntaxResult = SyntaxParser.Parse(tokens, startingNonTerminal);
            syntaxResult = cleaner.CleanSyntaxTree(syntaxResult);
            if (!syntaxResult.IsError && syntaxResult.Root != null)
            {
                var r = Visitor.VisitSyntaxTree(syntaxResult.Root, parsingContext);
                result.Result = r;
                result.SyntaxTree = syntaxResult.Root;
                result.IsError = false;
            }
            else
            {
                result.Errors = new List<ParseError>();
                result.Errors.AddRange(syntaxResult.Errors);
                result.IsError = true;
            }

            return result;
        }
    }
}