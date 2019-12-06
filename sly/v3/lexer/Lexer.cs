using System;
using System.Collections.Generic;
using sly.lexer;
using sly.v3.lexer.regex;

namespace sly.v3.lexer
{
    /// <summary>
    ///     T is the token type
    /// </summary>
    /// <typeparam name="T">T is the enum Token type</typeparam>
    public class Lexer<T> : ILexer<T> where T : struct
    {
        private readonly IList<TokenDefinition<T>> tokenDefinitions = new List<TokenDefinition<T>>();

        public void AddDefinition(TokenDefinition<T> tokenDefinition)
        {
            tokenDefinitions.Add(tokenDefinition);
        }


        public LexerResult<T> Tokenize(string source)
        {
            List<Token<T>> tokens = new List<Token<T>>();

            var currentIndex = 0;
            var currentLine = 1;
            var currentLineStartIndex = 0;
            Token<T> previousToken = null;

            while (currentIndex < source.Length)
            {
                var currentColumn = currentIndex - currentLineStartIndex + 1;
                TokenDefinition<T> matchedDefinition = null;
                var matchLength = 0;

                foreach (var rule in tokenDefinitions)
                {
                    // Parse regex
                    var pattern = rule.Regex.ToString();
                    var regex = RegEx.Parse(pattern);

                    var match = rule.Regex.Match(source.Substring(currentIndex));

                    if (match.Success && match.Index == 0)
                    {
                        matchedDefinition = rule;
                        matchLength = match.Length;
                        break;
                    }
                }

                if (matchedDefinition == null)
                {
                    return new LexerResult<T>(new LexicalError(currentLine, currentColumn, source[currentIndex]));
                }

                var value = source.Substring(currentIndex, matchLength);

                if (matchedDefinition.IsEndOfLine)
                {
                    currentLineStartIndex = currentIndex + matchLength;
                    currentLine++;
                }

                if (!matchedDefinition.IsIgnored)
                {
                    previousToken = new Token<T>(matchedDefinition.TokenID, value,
                        new TokenPosition(currentIndex, currentLine, currentColumn));
                    tokens.Add(previousToken);
                }

                currentIndex += matchLength;
            }

            TokenPosition position;
            if (previousToken != null)
            {
                position = previousToken.Position;
                position = new TokenPosition(position.Index + 1, position.Line, position.Column + previousToken.Value.Length);
            }
            else
            {
                position = new TokenPosition(0, 0, 0);
            }

            var eos = new Token<T>
            {
                Position = position
            };

            tokens.Add(eos);
            return new LexerResult<T>(tokens);
        }

        public LexerResult<T> Tokenize(ReadOnlyMemory<char> source)
        {
            throw new NotImplementedException();
        }
    }
}