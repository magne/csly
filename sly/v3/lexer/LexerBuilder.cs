using System;
using System.Collections.Generic;
using System.Linq;
using sly.buildresult;
using sly.lexer;
using sly.lexer.fsm;

namespace sly.v3.lexer
{
    internal static class LexerBuilder
    {
        internal static BuildResult<ILexer<TLexeme>> BuildLexer<TLexeme>(BuildResult<ILexer<TLexeme>> result, BuildExtension<TLexeme> extensionBuilder)
            where TLexeme : struct
        {
            var attributes = sly.lexer.LexerBuilder.GetLexemes(result);

            if (IsRegexLexer(attributes) && !IsGenericLexer(attributes))
            {
                return BuildRegexLexer(attributes, result);
            }

            return sly.lexer.LexerBuilder.Build(attributes, result, extensionBuilder);
        }

        private static bool IsRegexLexer<TLexeme>(IDictionary<TLexeme, List<LexemeAttribute>> attributes)
        {
            return attributes.Values.SelectMany(list => list)
                             .Any(lexeme => !string.IsNullOrEmpty(lexeme.Pattern));
        }

        private static bool IsGenericLexer<IN>(Dictionary<IN, List<LexemeAttribute>> attributes)
        {
            return attributes.Values.SelectMany(list => list)
                             .Any(lexeme => lexeme.GenericToken != default);
        }

        private static BuildResult<ILexer<IN>> BuildRegexLexer<IN>(Dictionary<IN, List<LexemeAttribute>> attributes,
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