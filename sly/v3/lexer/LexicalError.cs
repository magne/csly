﻿using sly.v3.parser;

namespace sly.v3.lexer
{
    internal class LexicalError : ParseError
    {
        public LexicalError(int line, int column, char unexpectedChar)
        {
            Line = line;
            Column = column;
            UnexpectedChar = unexpectedChar;
        }

        public char UnexpectedChar { get; set; }

        public override string ErrorMessage =>
            $"Lexical Error : Unrecognized symbol '{UnexpectedChar}' at  (line {Line}, column {Column}).";

        public override string ToString()
        {
            return ErrorMessage;
        }
    }
}