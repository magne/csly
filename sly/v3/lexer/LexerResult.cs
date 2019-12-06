using System.Collections.Generic;

namespace sly.v3.lexer
{
    internal class LexerResult<IN> where IN : struct
    {
        public bool IsError { get; set; }

        public bool IsOk => !IsError;

        public LexicalError Error { get; }

        public List<Token<IN>> Tokens { get; set; }

        public LexerResult(List<Token<IN>> tokens)
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