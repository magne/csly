using System.Collections.Generic;
namespace sly.lexer
{
    public class LexerResult<TLexeme> where TLexeme : struct
    {
        public bool IsError { get; set; }

        public bool IsOk => !IsError;

        public LexicalError Error { get; }

        public List<Token<TLexeme>> Tokens { get; set; }

        public LexerResult(List<Token<TLexeme>> tokens)
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