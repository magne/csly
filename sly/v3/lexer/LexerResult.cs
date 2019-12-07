using System.Collections.Generic;

namespace sly.v3.lexer
{
    internal class LexerResult<TIn> where TIn : struct
    {
        public bool IsError { get; set; }

        public bool IsOk => !IsError;

        public LexicalError Error { get; }

        public List<Token<TIn>> Tokens { get; set; }

        public LexerResult(List<Token<TIn>> tokens)
        {
            IsError = false;
            Tokens = tokens;
        }

        public LexerResult(LexicalError error)
        {
            IsError = true;
            Error = error;
        }
    }
}