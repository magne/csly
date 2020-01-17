using System.Collections.Generic;
using sly.parser.syntax.tree;

namespace sly.parser
{
    public class SyntaxParseResult<TIn> where TIn : struct
    {
        public ISyntaxNode<TIn> Root { get; set; }

        public bool IsError { get; set; }

        public bool IsOk => !IsError;

        public List<UnexpectedTokenSyntaxError<TIn>> Errors { get; set; } = new List<UnexpectedTokenSyntaxError<TIn>>();

        public int EndingPosition { get; set; }

        public bool IsEnded { get; set; }
    }
}