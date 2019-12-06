using System;
using System.Collections.Generic;
using sly.buildresult;

namespace sly.v3.lexer
{
    internal static class LexerBuilder
    {
        internal static BuildResult<ILexer<IN>> BuildRegexLexer<IN>(Dictionary<IN, List<LexemeAttribute>> attributes,
                                                                   BuildResult<ILexer<IN>> result) where IN : struct
        {
            var lexer = new Lexer<IN>();
            foreach (var pair in attributes)
            {
                var tokenID = pair.Key;

                var lexemes = pair.Value;

                if (lexemes != null)
                {
                    try
                    {
                        foreach (var lexeme in lexemes)
                        {
                            lexer.AddDefinition(new TokenDefinition<IN>(tokenID,
                                                                        lexeme.Pattern,
                                                                        lexeme.IsSkippable,
                                                                        lexeme.IsLineEnding));
                        }
                    }
                    catch (Exception e)
                    {
                        result.AddError(new LexerInitializationError(ErrorLevel.ERROR,
                                                                     $"error at lexem {tokenID} : {e.Message}"));
                    }
                }
                else if (!tokenID.Equals(default(IN)))
                {
                    result.AddError(new LexerInitializationError(ErrorLevel.WARN,
                                                                 $"token {tokenID} in lexer definition {typeof(IN).FullName} does not have Lexeme"));
                }
            }

            result.Result = lexer;
            return result;
        }
    }
}