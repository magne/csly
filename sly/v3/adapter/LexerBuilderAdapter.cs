using System;
using System.Collections.Generic;
using System.Linq;
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
        internal static BuildResult<sly.lexer.ILexer<TLexeme>> BuildLexer<TLexeme>(BuildResult<sly.lexer.ILexer<TLexeme>> resultV2, sly.lexer.fsm.BuildExtension<TLexeme> extensionBuilder)
            where TLexeme : struct
        {
            var attributesV2 = sly.lexer.LexerBuilder.GetLexemes(resultV2);

            if (IsRegexLexer(attributesV2) && !IsGenericLexer(attributesV2))
            {
                var resultV3 = new BuildResult<ILexer<TLexeme>>();
                var attributesV3 = GetLexemes(resultV3);
                var res = LexerBuilder.BuildRegexLexer(attributesV3, resultV3);
                var result = new BuildResult<sly.lexer.ILexer<TLexeme>>(new LexerAdapter<TLexeme>(res.Result));
                result.AddErrors(res.Errors);
                return result;
            }

            return sly.lexer.LexerBuilder.Build(attributesV2, resultV2, extensionBuilder);
        }

        private static Dictionary<IN, List<LexemeAttribute>> GetLexemes<IN>(BuildResult<ILexer<IN>> result) where IN : struct
        {
            var attributes = new Dictionary<IN, List<LexemeAttribute>>();

            var values = Enum.GetValues(typeof(IN));
            foreach (Enum value in values)
            {
                var tokenID = (IN) (object) value;
                var enumAttributes = value.GetAttributesOfType<sly.lexer.LexemeAttribute>();
                if (enumAttributes.Length == 0)
                {
                    result?.AddError(new LexerInitializationError(ErrorLevel.WARN,
                        $"token {tokenID} in lexer definition {typeof(IN).FullName} does not have Lexeme"));
                }
                else
                {
                    attributes[tokenID] = enumAttributes.Select(ConvertLexemeAttribute).ToList();
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

        private static bool IsRegexLexer<TLexeme>(IDictionary<TLexeme, List<sly.lexer.LexemeAttribute>> attributes)
        {
            return attributes.Values.SelectMany(list => list)
                .Any(lexeme => !string.IsNullOrEmpty(lexeme.Pattern));
        }

        private static bool IsGenericLexer<IN>(Dictionary<IN, List<sly.lexer.LexemeAttribute>> attributes)
        {
            return attributes.Values.SelectMany(list => list)
                .Any(lexeme => lexeme.GenericToken != default);
        }

        private class LexerAdapter<IN> : sly.lexer.ILexer<IN> where IN : struct
        {
            private readonly ILexer<IN> lexer;

            public LexerAdapter(ILexer<IN> lexer)
            {
                this.lexer = lexer;
            }

            public void AddDefinition(sly.lexer.TokenDefinition<IN> tokenDefinition)
            { }

            public sly.lexer.LexerResult<IN> Tokenize(string source)
            {
                var resultV3 = lexer.Tokenize(source);
                if (resultV3.IsError)
                {
                    return new sly.lexer.LexerResult<IN>(ConvertLexicalError(resultV3.Error));
                }

                return new sly.lexer.LexerResult<IN>(resultV3.Tokens.Select(ConvertToken).ToList());
            }

            public sly.lexer.LexerResult<IN> Tokenize(ReadOnlyMemory<char> source)
            {
                var resultV3 = lexer.Tokenize(source);
                if (resultV3.IsError)
                {
                    return new sly.lexer.LexerResult<IN>(ConvertLexicalError(resultV3.Error));
                }

                return new sly.lexer.LexerResult<IN>(resultV3.Tokens.Select(ConvertToken).ToList());
            }

            private static sly.lexer.LexicalError ConvertLexicalError(LexicalError error)
            {
                return new sly.lexer.LexicalError(error.Line, error.Column, error.UnexpectedChar);
            }

            private static sly.lexer.Token<IN> ConvertToken(Token<IN> token)
            {
                var position = new sly.lexer.TokenPosition(token.Position.Index, token.Position.Line, token.Position.Column);
                if (token.IsEOS)
                {
                    return new sly.lexer.Token<IN>()
                    {
                        Position = position
                    };
                }

                var commentType = (sly.lexer.CommentType) token.CommentType;
                return new sly.lexer.Token<IN>(token.TokenID, token.SpanValue, position, commentType: commentType);
            }
        }
    }
}