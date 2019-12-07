using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using sly.buildresult;
using sly.v3.lexer.fsm;

namespace sly.v3.lexer
{
    public static class EnumHelper
    {
        /// <summary>
        ///     Gets an attribute on an enum field value
        /// </summary>
        /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
        /// <param name="enumVal">The enum value</param>
        /// <returns>The attributes of type T that exist on the enum value, or an empty array if no such attributes are found.</returns>
        /// <example>var attrs = myEnumVariable.GetAttributesOfType&lt;DescriptionAttribute&gt;();</example>
        public static T[] GetAttributesOfType<T>(this Enum enumVal) where T : Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = (T[]) memInfo[0].GetCustomAttributes(typeof(T), false);

            return attributes;
        }
    }

    internal static class LexerBuilder
    {
        public static BuildResult<ILexer<TIn>> BuildLexer<TIn>(BuildResult<ILexer<TIn>> result,
                                                             LexerAttribute lexerAttribute,
                                                             Dictionary<TIn, List<LexemeAttribute>> attributes,
                                                             Dictionary<TIn, List<CommentAttribute>> commentAttributes,
                                                             BuildExtension<TIn> extensionBuilder = null) where TIn : struct
        {
            attributes ??= GetLexemes(result);

            result = Build(attributes, lexerAttribute, commentAttributes, result, extensionBuilder);

            return result;
        }

        private static Dictionary<TIn, List<LexemeAttribute>> GetLexemes<TIn>(BuildResult<ILexer<TIn>> result) where TIn : struct
        {
            var attributes = new Dictionary<TIn, List<LexemeAttribute>>();

            var values = Enum.GetValues(typeof(TIn));
            foreach (Enum value in values)
            {
                var tokenId = (TIn) (object) value;
                var enumAttributes = value.GetAttributesOfType<LexemeAttribute>();
                if (enumAttributes.Length == 0)
                {
                    result?.AddError(new LexerInitializationError(ErrorLevel.WARN,
                                                                  $"token {tokenId} in lexer definition {typeof(TIn).FullName} does not have Lexeme"));
                }
                else
                {
                    attributes[tokenId] = enumAttributes.ToList();
                }
            }

            return attributes;
        }

        private static BuildResult<ILexer<TIn>> Build<TIn>(Dictionary<TIn, List<LexemeAttribute>> attributes,
                                                         LexerAttribute lexerAttribute,
                                                         Dictionary<TIn, List<CommentAttribute>> commentAttributes,
                                                         BuildResult<ILexer<TIn>> result,
                                                         BuildExtension<TIn> extensionBuilder = null) where TIn : struct
        {
            var hasRegexLexemes = IsRegexLexer(attributes);
            var hasGenericLexemes = IsGenericLexer(attributes);

            if (hasGenericLexemes && hasRegexLexemes)
            {
                result.AddError(new LexerInitializationError(ErrorLevel.WARN,
                                                             "cannot mix Regex lexemes and Generic lexemes in same lexer"));
            }
            else
            {
                if (hasRegexLexemes)
                {
                    result = BuildRegexLexer(attributes, result);
                }
                else if (hasGenericLexemes)
                {
                    result = BuildGenericLexer(attributes, lexerAttribute, commentAttributes, extensionBuilder, result);
                }
            }

            return result;
        }

        private static bool IsRegexLexer<TIn>(Dictionary<TIn, List<LexemeAttribute>> attributes)
        {
            return attributes.Values.SelectMany(list => list)
                             .Any(lexeme => !string.IsNullOrEmpty(lexeme.Pattern));
        }

        private static bool IsGenericLexer<TIn>(Dictionary<TIn, List<LexemeAttribute>> attributes)
        {
            return attributes.Values.SelectMany(list => list)
                             .Any(lexeme => lexeme.GenericToken != default);
        }

        private static BuildResult<ILexer<TIn>> BuildRegexLexer<TIn>(Dictionary<TIn, List<LexemeAttribute>> attributes,
                                                                   BuildResult<ILexer<TIn>> result) where TIn : struct
        {
            var lexer = new Lexer<TIn>();
            foreach (var pair in attributes)
            {
                var tokenId = pair.Key;

                var lexemes = pair.Value;

                if (lexemes != null)
                {
                    try
                    {
                        foreach (var lexeme in lexemes)
                        {
                            lexer.AddDefinition(new TokenDefinition<TIn>(tokenId,
                                                                        lexeme.Pattern,
                                                                        lexeme.IsSkippable,
                                                                        lexeme.IsLineEnding));
                        }
                    }
                    catch (Exception e)
                    {
                        result.AddError(new LexerInitializationError(ErrorLevel.ERROR,
                                                                     $"error at lexem {tokenId} : {e.Message}"));
                    }
                }
                else if (!tokenId.Equals(default(TIn)))
                {
                    result.AddError(new LexerInitializationError(ErrorLevel.WARN,
                                                                 $"token {tokenId} in lexer definition {typeof(TIn).FullName} does not have Lexeme"));
                }
            }

            result.Result = lexer;
            return result;
        }

        private static BuildResult<ILexer<TIn>> BuildGenericLexer<TIn>(Dictionary<TIn, List<LexemeAttribute>> attributes,
                                                                     LexerAttribute lexerAttribute,
                                                                     Dictionary<TIn, List<CommentAttribute>> commentAttributes,
                                                                     BuildExtension<TIn> extensionBuilder,
                                                                     BuildResult<ILexer<TIn>> result) where TIn : struct
        {
            var (config, tokens) = GetConfigAndGenericTokens(lexerAttribute, attributes);
            result = CheckStringAndCharTokens(attributes, result);
            config.ExtensionBuilder = extensionBuilder;
            var lexer = new GenericLexer<TIn>(config, tokens);
            var extensions = new Dictionary<TIn, LexemeAttribute>();
            foreach (var pair in attributes)
            {
                var tokenId = pair.Key;

                var lexemes = pair.Value;
                foreach (var lexeme in lexemes)
                {
                    try
                    {
                        if (lexeme.IsStaticGeneric)
                        {
                            lexer.AddLexeme(lexeme.GenericToken, tokenId);
                        }

                        if (lexeme.IsKeyWord)
                        {
                            foreach (var param in lexeme.GenericTokenParameters)
                            {
                                lexer.AddKeyWord(tokenId, param);
                            }
                        }

                        if (lexeme.IsSugar)
                        {
                            foreach (var param in lexeme.GenericTokenParameters)
                            {
                                lexer.AddSugarLexem(tokenId, param);
                            }
                        }

                        if (lexeme.IsString)
                        {
                            var (delimiter, escape) = GetDelimiters(lexeme, "\"", "\\");
                            lexer.AddStringLexem(tokenId, delimiter, escape);
                        }

                        if (lexeme.IsChar)
                        {
                            var (delimiter, escape) = GetDelimiters(lexeme, "'", "\\");
                            lexer.AddCharLexem(tokenId, delimiter, escape);
                        }

                        if (lexeme.IsExtension)
                        {
                            extensions[tokenId] = lexeme;
                        }
                    }
                    catch (Exception e)
                    {
                        result.AddError(new InitializationError(ErrorLevel.FATAL, e.Message));
                    }
                }
            }

            AddExtensions(extensions, extensionBuilder, lexer);

            var comments = GetCommentsAttribute(result, commentAttributes);
            if (!result.IsError)
            {
                foreach (var comment in comments)
                {
                    NodeCallback<GenericToken> callbackSingle = match =>
                    {
                        match.Properties[GenericLexer<TIn>.DerivedToken] = comment.Key;
                        match.Result.IsComment = true;
                        match.Result.CommentType = CommentType.Single;
                        return match;
                    };

                    NodeCallback<GenericToken> callbackMulti = match =>
                    {
                        match.Properties[GenericLexer<TIn>.DerivedToken] = comment.Key;
                        match.Result.IsComment = true;
                        match.Result.CommentType = CommentType.Multi;
                        return match;
                    };

                    foreach (var commentAttr in comment.Value)
                    {
                        var fsmBuilder = lexer.FSMBuilder;

                        var hasSingleLine = !string.IsNullOrWhiteSpace(commentAttr.SingleLineCommentStart);
                        if (hasSingleLine)
                        {
                            lexer.SingleLineComment = commentAttr.SingleLineCommentStart;

                            fsmBuilder.GoTo(GenericLexer<TIn>.start);
                            fsmBuilder.ConstantTransition(commentAttr.SingleLineCommentStart);
                            fsmBuilder.Mark(GenericLexer<TIn>.single_line_comment_start);
                            fsmBuilder.End(GenericToken.Comment);
                            fsmBuilder.CallBack(callbackSingle);
                        }

                        var hasMultiLine = !string.IsNullOrWhiteSpace(commentAttr.MultiLineCommentStart);
                        if (hasMultiLine)
                        {
                            lexer.MultiLineCommentStart = commentAttr.MultiLineCommentStart;
                            lexer.MultiLineCommentEnd = commentAttr.MultiLineCommentEnd;

                            fsmBuilder.GoTo(GenericLexer<TIn>.start);
                            fsmBuilder.ConstantTransition(commentAttr.MultiLineCommentStart);
                            fsmBuilder.Mark(GenericLexer<TIn>.multi_line_comment_start);
                            fsmBuilder.End(GenericToken.Comment);
                            fsmBuilder.CallBack(callbackMulti);
                        }
                    }
                }
            }

            result.Result = lexer;
            return result;
        }

        private static (GenericLexer<TIn>.Config, GenericToken[]) GetConfigAndGenericTokens<TIn>(LexerAttribute lexerAttribute, IDictionary<TIn, List<LexemeAttribute>> attributes)
            where TIn : struct
        {
            var config = new GenericLexer<TIn>.Config();
            lexerAttribute ??= typeof(TIn).GetCustomAttribute<LexerAttribute>();
            if (lexerAttribute != null)
            {
                config.IgnoreWS = lexerAttribute.IgnoreWS;
                config.IgnoreEOL = lexerAttribute.IgnoreEOL;
                config.WhiteSpace = lexerAttribute.WhiteSpace;
                config.KeyWordIgnoreCase = lexerAttribute.KeyWordIgnoreCase;
            }

            var statics = new List<GenericToken>();
            foreach (var lexeme in attributes.Values.SelectMany(list => list))
            {
                statics.Add(lexeme.GenericToken);
                if (lexeme.IsIdentifier)
                {
                    config.IdType = lexeme.IdentifierType;
                    if (lexeme.IdentifierType == IdentifierType.Custom)
                    {
                        config.IdentifierStartPattern = ParseIdentifierPattern(lexeme.IdentifierStartPattern);
                        config.IdentifierRestPattern = ParseIdentifierPattern(lexeme.IdentifierRestPattern);
                    }
                }
            }

            return (config, statics.Distinct().ToArray());
        }

        private static IEnumerable<char[]> ParseIdentifierPattern(string pattern)
        {
            var index = 0;
            while (index < pattern.Length)
            {
                if (index <= pattern.Length - 3 && pattern[index + 1] == '-')
                {
                    if (pattern[index] < pattern[index + 2])
                    {
                        yield return new[] { pattern[index], pattern[index + 2] };
                    }
                    else
                    {
                        yield return new[] { pattern[index + 2], pattern[index] };
                    }

                    index += 3;
                }
                else
                {
                    yield return new[] { pattern[index++] };
                }
            }
        }

        private static BuildResult<ILexer<TIn>> CheckStringAndCharTokens<TIn>(
            Dictionary<TIn, List<LexemeAttribute>> attributes, BuildResult<ILexer<TIn>> result) where TIn : struct
        {
            var allLexemes = attributes.Values.SelectMany(a => a);

            var allDelimiters = allLexemes
                                .Where(a => a.IsString || a.IsChar)
                                .Where(a => a.HasGenericTokenParameters)
                                .Select(a => a.GenericTokenParameters[0]);

            var duplicates = allDelimiters.GroupBy(x => x)
                                          .Where(g => g.Count() > 1)
                                          .Select(y => new { Element = y.Key, Counter = y.Count() });

            foreach (var duplicate in duplicates)
            {
                result.AddError(new LexerInitializationError(ErrorLevel.FATAL,
                                                             $"char or string lexeme dilimiter {duplicate.Element} is used {duplicate.Counter} times. This will results in lexing conflicts"));
            }

            return result;
        }

        private static (string delimiter, string escape) GetDelimiters(LexemeAttribute lexeme, string delimiter, string escape)
        {
            if (lexeme.HasGenericTokenParameters)
            {
                delimiter = lexeme.GenericTokenParameters[0];
                if (lexeme.GenericTokenParameters.Length > 1)
                {
                    escape = lexeme.GenericTokenParameters[1];
                }
            }

            return (delimiter, escape);
        }

        private static void AddExtensions<TIn>(Dictionary<TIn, LexemeAttribute> extensions,
                                              BuildExtension<TIn> extensionBuilder, GenericLexer<TIn> lexer) where TIn : struct
        {
            if (extensionBuilder != null)
            {
                foreach (var attr in extensions)
                {
                    extensionBuilder(attr.Key, attr.Value, lexer);
                }
            }
        }

        private static Dictionary<TIn, List<CommentAttribute>> GetCommentsAttribute<TIn>(BuildResult<ILexer<TIn>> result, Dictionary<TIn, List<CommentAttribute>> attributes) where TIn : struct
        {
            if (attributes == null)
            {
                attributes = new Dictionary<TIn, List<CommentAttribute>>();

                var values = Enum.GetValues(typeof(TIn));
                foreach (Enum value in values)
                {
                    var tokenId = (TIn) (object) value;
                    var enumAttributes = value.GetAttributesOfType<CommentAttribute>();
                    if (enumAttributes != null && enumAttributes.Any()) attributes[tokenId] = enumAttributes.ToList();
                }
            }

            var commentCount = attributes.Values.Select(l => l?.Count(attr => attr.GetType() == typeof(CommentAttribute)) ?? 0).Sum();
            var multiLineCommentCount = attributes.Values.Select(l => l?.Count(attr => attr.GetType() == typeof(MultiLineCommentAttribute)) ?? 0).Sum();
            var singleLineCommentCount = attributes.Values.Select(l => l?.Count(attr => attr.GetType() == typeof(SingleLineCommentAttribute)) ?? 0).Sum();

            if (commentCount > 1)
            {
                result.AddError(new LexerInitializationError(ErrorLevel.FATAL, "too many comment lexem"));
            }

            if (multiLineCommentCount > 1)
            {
                result.AddError(new LexerInitializationError(ErrorLevel.FATAL, "too many multi-line comment lexem"));
            }

            if (singleLineCommentCount > 1)
            {
                result.AddError(new LexerInitializationError(ErrorLevel.FATAL, "too many single-line comment lexem"));
            }

            if (commentCount > 0 && (multiLineCommentCount > 0 || singleLineCommentCount > 0))
            {
                result.AddError(new LexerInitializationError(ErrorLevel.FATAL, "comment lexem can't be used together with single-line or multi-line comment lexems"));
            }

            return attributes;
        }
    }
}