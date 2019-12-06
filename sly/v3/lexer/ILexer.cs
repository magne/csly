using System;

namespace sly.v3.lexer
{
    internal interface ILexer<T> where T : struct
    {
        void AddDefinition(TokenDefinition<T> tokenDefinition);
        LexerResult<T> Tokenize(string source);

        LexerResult<T> Tokenize(ReadOnlyMemory<char> source);
    }
}