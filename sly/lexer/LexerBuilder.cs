using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using sly.buildresult;
using sly.lexer.fsm;

namespace sly.lexer
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

    public class LexerBuilder
    {
        public static bool UseV3 { get; set; }

        static LexerBuilder()
        {
            UseV3 = Environment.GetEnvironmentVariable("SLY_V3") != null;
        }

        public static Dictionary<TLexeme, List<LexemeAttribute>> GetLexemes<TLexeme>(BuildResult<ILexer<TLexeme>> result) where TLexeme: struct
        {
            var attributes = new Dictionary<TLexeme, List<LexemeAttribute>>();

            var values = Enum.GetValues(typeof(TLexeme));
            foreach (Enum value in values)
            {
                var tokenId = (TLexeme) (object) value;
                var enumAttributes = value.GetAttributesOfType<LexemeAttribute>();
                if (enumAttributes.Length == 0)
                {
                    result?.AddError(new LexerInitializationError(ErrorLevel.WARN,
                        $"token {tokenId} in lexer definition {typeof(TLexeme).FullName} does not have Lexeme"));
                }
                else
                {
                    attributes[tokenId] = enumAttributes.ToList();
                }
            }

            return attributes;
        }

        public static BuildResult<ILexer<IN>> BuildLexer<IN>(BuildExtension<IN> extensionBuilder = null) where IN : struct
        {
            return BuildLexer(new BuildResult < ILexer < IN >>() , extensionBuilder);
        }

        public static BuildResult<ILexer<TLexeme>> BuildLexer<TLexeme>(BuildResult<ILexer<TLexeme>> result,
                                                             BuildExtension<TLexeme> extensionBuilder = null) where TLexeme : struct
        {
            if (UseV3)
            {
                return v3.adapter.LexerBuilderAdapter.BuildLexer(result, extensionBuilder);
            }

            var attributes = GetLexemes(result);

            result = Build(attributes, result, extensionBuilder);

            return result;
        }


        internal static BuildResult<ILexer<TLexeme>> Build<TLexeme>(Dictionary<TLexeme, List<LexemeAttribute>> attributes,
            BuildResult<ILexer<TLexeme>> result, BuildExtension<TLexeme> extensionBuilder = null) where TLexeme : struct
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
                    result = BuildGenericLexer(attributes, extensionBuilder, result);
                }
            }

            return result;
        }

        private static bool IsRegexLexer<TLexeme>(Dictionary<TLexeme, List<LexemeAttribute>> attributes)
        {
            return attributes.Values.SelectMany(list => list)
                             .Any(lexeme => !string.IsNullOrEmpty(lexeme.Pattern));
        }

        private static bool IsGenericLexer<TLexeme>(Dictionary<TLexeme, List<LexemeAttribute>> attributes)
        {
            return attributes.Values.SelectMany(list => list)
                             .Any(lexeme => lexeme.GenericToken != default(GenericToken));
        }


        private static BuildResult<ILexer<TLexeme>> BuildRegexLexer<TLexeme>(Dictionary<TLexeme, List<LexemeAttribute>> attributes,
            BuildResult<ILexer<TLexeme>> result) where TLexeme : struct
        {
            var lexer = new Lexer<TLexeme>();
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
                            lexer.AddDefinition(new TokenDefinition<TLexeme>(tokenId, lexeme.Pattern, lexeme.IsSkippable,
                                lexeme.IsLineEnding));
                        }
                    }
                    catch (Exception e)
                    {
                        result.AddError(new LexerInitializationError(ErrorLevel.ERROR,
                            $"error at lexem {tokenId} : {e.Message}"));
                    }
                }
                else if (!tokenId.Equals(default(TLexeme)))
                {
                    result.AddError(new LexerInitializationError(ErrorLevel.WARN,
                        $"token {tokenId} in lexer definition {typeof(TLexeme).FullName} does not have Lexeme"));
                }
            }

            result.Result = lexer;
            return result;
        }

        private static (GenericLexer<TLexeme>.Config, GenericToken[]) GetConfigAndGenericTokens<TLexeme>(IDictionary<TLexeme, List<LexemeAttribute>> attributes)
            where TLexeme : struct
        {
            var config = new GenericLexer<TLexeme>.Config();
            var lexerAttribute = typeof(TLexeme).GetCustomAttribute<LexerAttribute>();
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

        private static BuildResult<ILexer<TLexeme>> BuildGenericLexer<TLexeme>(Dictionary<TLexeme, List<LexemeAttribute>> attributes,
                                                                     BuildExtension<TLexeme> extensionBuilder, BuildResult<ILexer<TLexeme>> result) where TLexeme : struct
        {
            result = CheckStringAndCharTokens(attributes, result);
            var (config, tokens) = GetConfigAndGenericTokens(attributes);
            config.ExtensionBuilder = extensionBuilder;
            var lexer = new GenericLexer<TLexeme>(config, tokens);
            var extensions = new Dictionary<TLexeme, LexemeAttribute>();
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

            var comments = GetCommentsAttribute(result);
            if (!result.IsError)
            {
                foreach (var comment in comments)
                {
                    NodeCallback<GenericToken> callbackSingle = match =>
                    {
                        match.Properties[GenericLexer<TLexeme>.DerivedToken] = comment.Key;
                        match.Result.IsComment = true;
                        match.Result.CommentType = CommentType.Single;
                        return match;
                    };

                    NodeCallback<GenericToken> callbackMulti = match =>
                    {
                        match.Properties[GenericLexer<TLexeme>.DerivedToken] = comment.Key;
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

                            fsmBuilder.GoTo(GenericLexer<TLexeme>.start);
                            fsmBuilder.ConstantTransition(commentAttr.SingleLineCommentStart);
                            fsmBuilder.Mark(GenericLexer<TLexeme>.single_line_comment_start);
                            fsmBuilder.End(GenericToken.Comment);
                            fsmBuilder.CallBack(callbackSingle);
                        }

                        var hasMultiLine = !string.IsNullOrWhiteSpace(commentAttr.MultiLineCommentStart);
                        if (hasMultiLine)
                        {
                            lexer.MultiLineCommentStart = commentAttr.MultiLineCommentStart;
                            lexer.MultiLineCommentEnd = commentAttr.MultiLineCommentEnd;

                            fsmBuilder.GoTo(GenericLexer<TLexeme>.start);
                            fsmBuilder.ConstantTransition(commentAttr.MultiLineCommentStart);
                            fsmBuilder.Mark(GenericLexer<TLexeme>.multi_line_comment_start);
                            fsmBuilder.End(GenericToken.Comment);
                            fsmBuilder.CallBack(callbackMulti);
                        }
                    }
                }
            }

            result.Result = lexer;
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

        private static BuildResult<ILexer<TLexeme>> CheckStringAndCharTokens<TLexeme>(
            Dictionary<TLexeme, List<LexemeAttribute>> attributes, BuildResult<ILexer<TLexeme>> result) where TLexeme : struct
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


        private static Dictionary<TLexeme, List<CommentAttribute>> GetCommentsAttribute<TLexeme>(BuildResult<ILexer<TLexeme>> result) where TLexeme : struct
        {
            var attributes = new Dictionary<TLexeme, List<CommentAttribute>>();

            var values = Enum.GetValues(typeof(TLexeme));
            foreach (Enum value in values)
            {
                var tokenId = (TLexeme) (object) value;
                var enumAttributes = value.GetAttributesOfType<CommentAttribute>();
                if (enumAttributes != null && enumAttributes.Any()) attributes[tokenId] = enumAttributes.ToList();
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

        private static void AddExtensions<TLexeme>(Dictionary<TLexeme, LexemeAttribute> extensions,
            BuildExtension<TLexeme> extensionBuilder, GenericLexer<TLexeme> lexer) where TLexeme : struct
        {
            if (extensionBuilder != null)
            {
                foreach (var attr in extensions)
                {
                    extensionBuilder(attr.Key, attr.Value, lexer);
                }
            }
        }
    }
}