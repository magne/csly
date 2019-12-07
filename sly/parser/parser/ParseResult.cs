using System.Collections.Generic;
using sly.parser.syntax.tree;

namespace sly.parser
{
    public class ParseResult<TIn, TOut> where TIn : struct
    {
        public TOut Result { get; set; }

        public ISyntaxNode<TIn> SyntaxTree { get; set; }

        public bool IsError { get; set; }

        public bool IsOk => !IsError;

        public List<ParseError> Errors { get; set; }
    }
}