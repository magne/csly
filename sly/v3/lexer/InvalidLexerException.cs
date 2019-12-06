using System;

namespace sly.v3.lexer
{
    internal class InvalidLexerException : Exception
    {
        public InvalidLexerException(string message) : base(message)
        {
        }
    }
}