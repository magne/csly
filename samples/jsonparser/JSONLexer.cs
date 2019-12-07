using System;
using System.Collections.Generic;
using sly.lexer;
using sly.lexer.fsm;

namespace jsonparser
{
    // ReSharper disable once InconsistentNaming
    public class JSONLexer : ILexer<JsonToken>
    {
        public void AddDefinition(TokenDefinition<JsonToken> tokenDefinition)
        {
        }

        public LexerResult<JsonToken> Tokenize(string source)
        {
            return Tokenize(new ReadOnlyMemory<char>(source.ToCharArray()));
        }

        public LexerResult<JsonToken> Tokenize(ReadOnlyMemory<char> source)
        {
            var tokens = new List<Token<JsonToken>>();
            var position = 0;
            var length = source.Length;

            var currentTokenLine = 1;
            var currentTokenColumn = 1;
            var currentTokenPosition = 1;
            var currentValue = "";

            var inString = false;
            var inNum = false;
            var inNull = false;
            var inTrue = false;
            var inFalse = false;
            var numIsDouble = false;

            int tokenStartIndex = 0;
            int tokenLength = 0;

            var line = currentTokenLine;
            var column = currentTokenColumn;
            Func<JsonToken, Token<JsonToken>> NewToken = tok =>
            {
                var token = new Token<JsonToken>();
                token.Position = new TokenPosition(currentTokenPosition, line, column);
                token.SpanValue = source.Slice(tokenStartIndex,tokenLength);
                tokenStartIndex = tokenStartIndex + tokenLength;
                token.TokenID = tok;
                tokens.Add(token);
                currentValue = "";
                return token;
            };


            while (position < length)
            {
                var current = source.At(position);
                if (inString)
                {
                    currentValue += current;
                    if (current == '"')
                    {
                        inString = false;

                        NewToken(JsonToken.STRING);
                        position++;
                    }
                    else
                    {
                        position++;
                    }
                }
                else if (inNum)
                {
                    if (current == '.')
                    {
                        numIsDouble = true;
                        currentValue += current;
                    }
                    else if (char.IsDigit(current))
                    {
                        currentValue += current;
                        var type = numIsDouble ? JsonToken.DOUBLE : JsonToken.INT;
                        if (position == length - 1) NewToken(type);
                    }
                    else
                    {
                        inNum = false;
                        var type = numIsDouble ? JsonToken.DOUBLE : JsonToken.INT;
                        NewToken(type);
                        position--;
                    }

                    position++;
                }
                else if (inNull)
                {
                    if (current == "null"[currentValue.Length])
                    {
                        currentValue += current;
                        if (currentValue.Length == 4)
                        {
                            NewToken(JsonToken.NULL);
                            inNull = false;
                        }
                    }

                    position++;
                }
                else if (inFalse)
                {
                    if (current == "false"[currentValue.Length])
                    {
                        currentValue += current;
                        if (currentValue.Length == 5)
                        {
                            NewToken(JsonToken.BOOLEAN);
                            inFalse = false;
                        }
                        else
                        {
                            position++;
                        }
                    }
                }
                else if (inTrue)
                {
                    if (current == "true"[currentValue.Length])
                    {
                        currentValue += current;
                        if (currentValue.Length == 5)
                        {
                            NewToken(JsonToken.BOOLEAN);
                        }
                    }

                    position++;
                }
                else
                {
                    currentValue += current;
                    if (current == '"')
                    {
                        inString = true;
                        currentValue += current;
                    }
                    else if (char.IsDigit(current))
                    {
                        inNum = true;
                    }
                    else if (current == 't')
                    {
                        inTrue = true;
                    }
                    else if (current == 'f')
                    {
                        inFalse = true;
                    }
                    else if (current == 'n')
                    {
                        inNull = true;
                    }
                    else if (current == '[')
                    {
                        NewToken(JsonToken.CROG);
                    }
                    else if (current == ']')
                    {
                        NewToken(JsonToken.CROD);
                    }
                    else if (current == '{')
                    {
                        NewToken(JsonToken.ACCG);
                    }
                    else if (current == '}')
                    {
                        NewToken(JsonToken.ACCD);
                    }
                    else if (current == ':')
                    {
                        NewToken(JsonToken.COLON);
                    }
                    else if (current == ',')
                    {
                        NewToken(JsonToken.COMMA);
                    }
                    else if (char.IsWhiteSpace(current))
                    {
                        currentValue = "";
                    }
                    else if (current == '\n' || current == '\r')
                    {
                        currentValue = ";;";
                    }

                    position++;
                }
            }


            return new LexerResult<JsonToken>(tokens);
        }
    }
}