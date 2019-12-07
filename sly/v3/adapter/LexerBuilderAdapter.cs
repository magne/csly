using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using sly.buildresult;
using sly.v3.lexer;

namespace sly.v3.adapter
{
    internal static class EnumHelper
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

    internal static class LexerBuilderAdapter
    {
        private static bool useOldGenericLexer;

        internal static IDisposable UseOldGenericLexer()
        {
            useOldGenericLexer = true;
            return new ResetOldGenericLexer();
        }

        private class ResetOldGenericLexer : IDisposable
        {
            public void Dispose()
            {
                useOldGenericLexer = false;
            }
        }

        internal static BuildResult<sly.lexer.ILexer<TLexeme>> BuildLexer<TLexeme>(BuildResult<sly.lexer.ILexer<TLexeme>> resultV2, sly.lexer.fsm.BuildExtension<TLexeme> extensionBuilder)
            where TLexeme : struct
        {
            var attributesV2 = sly.lexer.LexerBuilder.GetLexemes(resultV2);

            if (useOldGenericLexer || extensionBuilder != null)
            {
                return sly.lexer.LexerBuilder.Build(attributesV2, resultV2, extensionBuilder);
            }

            var resultV3 = new BuildResult<ILexer<TLexeme>>();
            var lexerAttributeV3 = ConvertLexerAttribute(typeof(TLexeme).GetCustomAttribute<sly.lexer.LexerAttribute>());
            var attributesV3 = GetLexemes(resultV3);
            var commentAttributes = GetCommentAttributes<TLexeme>();
            var res = LexerBuilder.BuildLexer(resultV3, lexerAttributeV3, attributesV3, commentAttributes);
            var result = new BuildResult<sly.lexer.ILexer<TLexeme>>(new LexerAdapter<TLexeme>(res.Result));
            result.AddErrors(res.Errors);
            return result;
        }

        private static LexerAttribute ConvertLexerAttribute(sly.lexer.LexerAttribute lexerAttribute)
        {
            if (lexerAttribute == null)
            {
                return null;
            }

            return new LexerAttribute
            {
                IgnoreWS = lexerAttribute.IgnoreWS,
                IgnoreEOL = lexerAttribute.IgnoreEOL,
                WhiteSpace = lexerAttribute.WhiteSpace,
                KeyWordIgnoreCase = lexerAttribute.KeyWordIgnoreCase
            };
        }

        private static Dictionary<TIn, List<LexemeAttribute>> GetLexemes<TIn>(BuildResult<ILexer<TIn>> result) where TIn : struct
        {
            var attributes = new Dictionary<TIn, List<LexemeAttribute>>();

            var values = Enum.GetValues(typeof(TIn));
            foreach (Enum value in values)
            {
                var tokenId = (TIn) (object) value;
                var enumAttributes = value.GetAttributesOfType<sly.lexer.LexemeAttribute>();
                if (enumAttributes.Length == 0)
                {
                    result?.AddError(new LexerInitializationError(ErrorLevel.WARN,
                        $"token {tokenId} in lexer definition {typeof(TIn).FullName} does not have Lexeme"));
                }
                else
                {
                    attributes[tokenId] = enumAttributes.Select(ConvertLexemeAttribute).ToList();
                }
            }

            return attributes;
        }

        private static LexemeAttribute ConvertLexemeAttribute(sly.lexer.LexemeAttribute a)
        {
            if (a.GenericToken == default)
            {
                return new LexemeAttribute(a.Pattern, a.IsSkippable, a.IsLineEnding);
            }

            if (a.GenericToken == sly.lexer.GenericToken.Identifier)
            {
                return new LexemeAttribute((GenericToken) a.GenericToken, (IdentifierType) a.IdentifierType, a.IdentifierStartPattern, a.IdentifierRestPattern);
            }

            return new LexemeAttribute((GenericToken) a.GenericToken, a.GenericTokenParameters);
        }

        private static Dictionary<TIn, List<CommentAttribute>> GetCommentAttributes<TIn>() where TIn : struct
        {
            var attributes = new Dictionary<TIn, List<sly.lexer.CommentAttribute>>();

            var values = Enum.GetValues(typeof(TIn));
            foreach (Enum value in values)
            {
                var tokenId = (TIn) (object) value;
                var enumAttributes = value.GetAttributesOfType<sly.lexer.CommentAttribute>();
                if (enumAttributes != null && enumAttributes.Any()) attributes[tokenId] = enumAttributes.ToList();
            }

            return attributes.ToDictionary(pair => pair.Key, pair => pair.Value.Select(ConvertCommentAttribute).ToList());
        }

        private static CommentAttribute ConvertCommentAttribute(sly.lexer.CommentAttribute attribute)
        {
            if (attribute is sly.lexer.SingleLineCommentAttribute sl)
            {
                return new SingleLineCommentAttribute(sl.SingleLineCommentStart);
            }

            if (attribute is sly.lexer.MultiLineCommentAttribute ml)
            {
                return new MultiLineCommentAttribute(ml.MultiLineCommentStart, ml.MultiLineCommentEnd);
            }

            return new CommentAttribute(attribute.SingleLineCommentStart, attribute.MultiLineCommentStart, attribute.MultiLineCommentEnd);
        }

        private class LexerAdapter<TIn> : sly.lexer.ILexer<TIn> where TIn : struct
        {
            private readonly ILexer<TIn> lexer;

            public LexerAdapter(ILexer<TIn> lexer)
            {
                this.lexer = lexer;
            }

            public void AddDefinition(sly.lexer.TokenDefinition<TIn> tokenDefinition)
            { }

            public sly.lexer.LexerResult<TIn> Tokenize(string source)
            {
                var resultV3 = lexer.Tokenize(source);
                if (resultV3.IsError)
                {
                    return new sly.lexer.LexerResult<TIn>(ConvertLexicalError(resultV3.Error));
                }

                return new sly.lexer.LexerResult<TIn>(resultV3.Tokens.Select(ConvertToken).ToList());
            }

            public sly.lexer.LexerResult<TIn> Tokenize(ReadOnlyMemory<char> source)
            {
                var resultV3 = lexer.Tokenize(source);
                if (resultV3.IsError)
                {
                    return new sly.lexer.LexerResult<TIn>(ConvertLexicalError(resultV3.Error));
                }

                return new sly.lexer.LexerResult<TIn>(resultV3.Tokens.Select(ConvertToken).ToList());
            }

            private static sly.lexer.LexicalError ConvertLexicalError(LexicalError error)
            {
                return new sly.lexer.LexicalError(error.Line, error.Column, error.UnexpectedChar);
            }

            private static sly.lexer.Token<TIn> ConvertToken(Token<TIn> token)
            {
                var position = new sly.lexer.TokenPosition(token.Position.Index, token.Position.Line, token.Position.Column);
                if (token.IsEOS)
                {
                    return new sly.lexer.Token<TIn>()
                    {
                        Position = position
                    };
                }

                var commentType = (sly.lexer.CommentType) token.CommentType;
                return new sly.lexer.Token<TIn>(token.TokenID, token.SpanValue, position, commentType: commentType)
                {
                    IsComment = token.IsComment,
                    IsEmpty = token.IsEmpty,
                    Discarded = token.Discarded,
                    StringDelimiter = token.StringDelimiter,
                    CharDelimiter = token.CharDelimiter
                };
            }
        }
    }
}