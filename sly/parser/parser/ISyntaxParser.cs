using System.Collections.Generic;
using sly.lexer;
using sly.parser.generator;

namespace sly.parser
{
    public interface ISyntaxParser<TIn, TOut> where TIn : struct
    {
        string StartingNonTerminal { get; set; }

        SyntaxParseResult<TIn> Parse(IList<Token<TIn>> tokens, string startingNonTerminal = null);

        void Init(ParserConfiguration<TIn, TOut> configuration, string root);
    }
}