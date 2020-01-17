﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using sly.lexer.fsm;

namespace sly.lexer
{
    public enum GenericToken
    {
        Default,
        Identifier,
        Int,
        Double,
        KeyWord,
        String,
        Char,
        SugarToken,

        Extension,

        Comment
    }

    public enum IdentifierType
    {
        Alpha,
        AlphaNumeric,
        AlphaNumericDash,
        Custom
    }

    // ReSharper disable once InconsistentNaming
    public enum EOLType
    {
        Windows,
        Nix,

        Mac,
        Environment,

        No
    }

    public class GenericLexer<TLexeme> : ILexer<TLexeme> where TLexeme : struct
    {
        public class Config
        {
            public Config()
            {
                IdType = IdentifierType.Alpha;
                IgnoreEOL = true;
                IgnoreWS = true;
                WhiteSpace = new[] { ' ', '\t' };
            }

            public IdentifierType IdType { get; set; }

            // ReSharper disable once InconsistentNaming
            public bool IgnoreEOL { get; set; }

            // ReSharper disable once InconsistentNaming
            public bool IgnoreWS { get; set; }

            public char[] WhiteSpace { get; set; }

            public bool KeyWordIgnoreCase { get; set; }

            public IEnumerable<char[]> IdentifierStartPattern { get; set; }

            public IEnumerable<char[]> IdentifierRestPattern { get; set; }

            public BuildExtension<TLexeme> ExtensionBuilder { get; set; }

            public IEqualityComparer<string> KeyWordComparer => KeyWordIgnoreCase ? StringComparer.OrdinalIgnoreCase : null;
        }

        public const string in_string = "in_string";
        public const string string_end = "string_end";
        public const string start_char = "start_char";
        public const string escapeChar_char = "escapeChar_char";
        public const string unicode_char = "unicode_char";
        public const string in_char = "in_char";
        public const string end_char = "char_end";
        public const string start = "start";
        public const string in_int = "in_int";
        public const string start_double = "start_double";
        public const string in_double = "in_double";
        public const string in_identifier = "in_identifier";
        public const string token_property = "token";
        public const string DerivedToken = "derivedToken";
        public const string defaultIdentifierKey = "identifier";
        public const string escape_string = "escape_string";
        public const string escape_char = "escape_char";

        public const string single_line_comment_start = "single_line_comment_start";

        public const string multi_line_comment_start = "multi_line_comment_start";

        protected readonly Dictionary<GenericToken, Dictionary<string, TLexeme>> DerivedTokens;
        protected TLexeme doubleDerivedToken;
        protected char EscapeStringDelimiterChar;

        protected readonly BuildExtension<TLexeme> ExtensionBuilder;
        public FSMLexerBuilder<GenericToken> FSMBuilder;
        protected TLexeme identifierDerivedToken;
        protected TLexeme intDerivedToken;

        protected FSMLexer<GenericToken> LexerFsm;
        protected int StringCounter;
        protected int CharCounter;


        protected Dictionary<TLexeme, Func<Token<TLexeme>, Token<TLexeme>>> CallBacks = new Dictionary<TLexeme, Func<Token<TLexeme>, Token<TLexeme>>>();

        protected char StringDelimiterChar;

        private readonly IEqualityComparer<string> KeyWordComparer;

        public GenericLexer(IdentifierType idType = IdentifierType.Alpha,
                            BuildExtension<TLexeme> extensionBuilder = null,
                            params GenericToken[] staticTokens)
            : this(new Config { IdType = idType, ExtensionBuilder = extensionBuilder }, staticTokens)
        { }

        public GenericLexer(Config config, GenericToken[] staticTokens)
        {
            DerivedTokens = new Dictionary<GenericToken, Dictionary<string, TLexeme>>();
            ExtensionBuilder = config.ExtensionBuilder;
            KeyWordComparer = config.KeyWordComparer;
            InitializeStaticLexer(config, staticTokens);
        }

        public string SingleLineComment { get; set; }

        public string MultiLineCommentStart { get; set; }

        public string MultiLineCommentEnd { get; set; }

        public void AddCallBack(TLexeme token, Func<Token<TLexeme>, Token<TLexeme>> callback)
        {
            CallBacks[token] = callback;
        }

        public void AddDefinition(TokenDefinition<TLexeme> tokenDefinition) { }


        public LexerResult<TLexeme> Tokenize(string source)
        {
            var memorySource = new ReadOnlyMemory<char>(source.ToCharArray());
            return Tokenize(memorySource);
        }

        public LexerResult<TLexeme> Tokenize(ReadOnlyMemory<char> memorySource)
        {
            var tokens = new List<Token<TLexeme>>();

            var r = LexerFsm.Run(memorySource, 0);
            if (!r.IsSuccess && !r.IsEOS)
            {
                var result = r.Result;
                var error = new LexicalError(result.Position.Line, result.Position.Column, result.CharValue);
                return new LexerResult<TLexeme>(error);
            }

            while (r.IsSuccess)
            {
                var transcoded = Transcode(r);
                if (CallBacks.TryGetValue(transcoded.TokenID, out var callback))
                {
                    transcoded = callback(transcoded);
                }

                tokens.Add(transcoded);

                r = LexerFsm.Run(memorySource);
                if (!r.IsSuccess && !r.IsEOS)
                {
                    var result = r.Result;
                    var error = new LexicalError(result.Position.Line, result.Position.Column, result.CharValue);
                    return new LexerResult<TLexeme>(error);
                }

                if (r.IsSuccess && r.Result.IsComment) ConsumeComment(r.Result, memorySource);
            }

            var eos = new Token<TLexeme>();
            var prev = tokens.LastOrDefault();
            if (prev == null)
            {
                eos.Position = new TokenPosition(1, 0, 0);
            }
            else
            {
                eos.Position = new TokenPosition(prev.Position.Index + 1, prev.Position.Line,
                    prev.Position.Column + prev.Value.Length);
            }
            tokens.Add(eos);
            return new LexerResult<TLexeme>(tokens);
        }


        private void InitializeStaticLexer(Config config, GenericToken[] staticTokens)
        {
            FSMBuilder = new FSMLexerBuilder<GenericToken>();
            StringCounter = 0;

            // conf
            FSMBuilder
                .IgnoreWS(config.IgnoreWS)
                .WhiteSpace(config.WhiteSpace)
                .IgnoreEOL(config.IgnoreEOL);

            // start machine definition
            FSMBuilder.Mark(start);

            if (staticTokens.Contains(GenericToken.Identifier) || staticTokens.Contains(GenericToken.KeyWord))
            {
                InitializeIdentifier(config);
            }

            // numeric
            if (staticTokens.Contains(GenericToken.Int) || staticTokens.Contains(GenericToken.Double))
            {
                FSMBuilder = FSMBuilder.GoTo(start)
                    .RangeTransition('0', '9')
                    .Mark(in_int)
                    .RangeTransitionTo('0', '9', in_int)
                    .End(GenericToken.Int);
                if (staticTokens.Contains(GenericToken.Double))
                    FSMBuilder.Transition('.')
                        .Mark(start_double)
                        .RangeTransition('0', '9')
                        .Mark(in_double)
                        .RangeTransitionTo('0', '9', in_double)
                        .End(GenericToken.Double);
            }

            LexerFsm = FSMBuilder.Fsm;
        }


        private void InitializeIdentifier(Config config)
        {
            // identifier
            if (config.IdType == IdentifierType.Custom)
            {
                var marked = false;
                foreach (var pattern in config.IdentifierStartPattern)
                {
                    FSMBuilder.GoTo(start);
                    if (pattern.Length == 1)
                    {
                        if (marked)
                        {
                            FSMBuilder.TransitionTo(pattern[0], in_identifier);
                        }
                        else
                        {
                            FSMBuilder.Transition(pattern[0]).Mark(in_identifier).End(GenericToken.Identifier);
                            marked = true;
                        }
                    }
                    else
                    {
                        if (marked)
                        {
                            FSMBuilder.RangeTransitionTo(pattern[0], pattern[1], in_identifier);
                        }
                        else
                        {
                            FSMBuilder.RangeTransition(pattern[0], pattern[1]).Mark(in_identifier).End(GenericToken.Identifier);
                            marked = true;
                        }
                    }
                }

                foreach (var pattern in config.IdentifierRestPattern)
                {
                    if (pattern.Length == 1)
                    {
                        FSMBuilder.TransitionTo(pattern[0], in_identifier);
                    }
                    else
                    {
                        FSMBuilder.RangeTransitionTo(pattern[0], pattern[1], in_identifier);
                    }
                }
            }
            else
            {
                FSMBuilder
                    .GoTo(start)
                    .RangeTransition('a', 'z')
                    .Mark(in_identifier)
                    .GoTo(start)
                    .RangeTransitionTo('A', 'Z', in_identifier)
                    .RangeTransitionTo('a', 'z', in_identifier)
                    .RangeTransitionTo('A', 'Z', in_identifier)
                    .End(GenericToken.Identifier);

                if (config.IdType == IdentifierType.AlphaNumeric || config.IdType == IdentifierType.AlphaNumericDash)
                {
                    FSMBuilder
                        .GoTo(in_identifier)
                        .RangeTransitionTo('0', '9', in_identifier);
                }

                if (config.IdType == IdentifierType.AlphaNumericDash)
                {
                    FSMBuilder
                        .GoTo(start)
                        .TransitionTo('_', in_identifier)
                        .TransitionTo('_', in_identifier)
                        .TransitionTo('-', in_identifier);
                }
            }
        }

        public void AddLexeme(GenericToken generic, TLexeme token)
        {
            NodeCallback<GenericToken> callback = match =>
            {
                switch (match.Result.TokenID)
                {
                    case GenericToken.Identifier:
                        {
                            if (DerivedTokens.ContainsKey(GenericToken.Identifier))
                            {
                                var possibleTokens = DerivedTokens[GenericToken.Identifier];
                                if (possibleTokens.ContainsKey(match.Result.Value))
                                    match.Properties[DerivedToken] = possibleTokens[match.Result.Value];
                                else
                                    match.Properties[DerivedToken] = identifierDerivedToken;
                            }
                            else
                            {
                                match.Properties[DerivedToken] = identifierDerivedToken;
                            }

                            break;
                        }
                    case GenericToken.Int:
                        {
                            match.Properties[DerivedToken] = intDerivedToken;
                            break;
                        }
                    case GenericToken.Double:
                        {
                            match.Properties[DerivedToken] = doubleDerivedToken;
                            break;
                        }
                    default:
                        {
                            match.Properties[DerivedToken] = token;
                            break;
                        }
                }

                return match;
            };

            switch (generic)
            {
                case GenericToken.Double:
                    {
                        doubleDerivedToken = token;
                        FSMBuilder.GoTo(in_double);
                        FSMBuilder.CallBack(callback);
                        break;
                    }
                case GenericToken.Int:
                    {
                        intDerivedToken = token;
                        FSMBuilder.GoTo(in_int);
                        FSMBuilder.CallBack(callback);
                        break;
                    }
                case GenericToken.Identifier:
                    {
                        identifierDerivedToken = token;
                        FSMBuilder.GoTo(in_identifier);
                        FSMBuilder.CallBack(callback);
                        break;
                    }
            }
        }

        public void AddLexeme(GenericToken genericToken, TLexeme token, string specialValue)
        {
            if (genericToken == GenericToken.SugarToken)
            {
                AddSugarLexem(token, specialValue);
            }

            if (!DerivedTokens.TryGetValue(genericToken, out var tokensForGeneric))
            {
                if (genericToken == GenericToken.Identifier)
                {
                    tokensForGeneric = new Dictionary<string, TLexeme>(KeyWordComparer);
                }
                else
                {
                    tokensForGeneric = new Dictionary<string, TLexeme>();
                }

                DerivedTokens[genericToken] = tokensForGeneric;
            }

            tokensForGeneric[specialValue] = token;
        }

        public void AddKeyWord(TLexeme token, string keyword)
        {
            NodeCallback<GenericToken> callback = match =>
            {
                TLexeme derivedToken;
                if (DerivedTokens.TryGetValue(GenericToken.Identifier, out var derived))
                {
                    if (!derived.TryGetValue(match.Result.Value, out derivedToken))
                    {
                        derivedToken = identifierDerivedToken;
                    }
                }
                else
                {
                    derivedToken = identifierDerivedToken;
                }

                match.Properties[DerivedToken] = derivedToken;

                return match;
            };

            AddLexeme(GenericToken.Identifier, token, keyword);
            var node = FSMBuilder.GetNode(in_identifier);
            if (!FSMBuilder.Fsm.HasCallback(node.Id))
            {
                FSMBuilder.GoTo(in_identifier).CallBack(callback);
            }
        }


        public ReadOnlyMemory<char> diffCharEscaper(char escapeStringDelimiterChar, char stringDelimiterChar, ReadOnlyMemory<char> stringValue)
        {
            var value = stringValue;
            var i = 1;
            var substitutionHappened = false;
            var escaping = false;
            var r = string.Empty;
            while (i < value.Length - 1)
            {
                var current = value.At(i);
                if (current == escapeStringDelimiterChar && i < value.Length - 2)
                {
                    escaping = true;
                    if (!substitutionHappened)
                    {
                        r = value.Slice(0, i).ToString();
                        substitutionHappened = true;
                    }
                }
                else
                {
                    if (escaping)
                    {
                        if (current != stringDelimiterChar)
                        {
                            r += escapeStringDelimiterChar;
                        }
                        escaping = false;
                    }
                    if (substitutionHappened)
                    {
                        r += current;
                    }
                }
                i++;
            }
            if (substitutionHappened)
            {
                r += value.At(value.Length - 1);
                value = r.AsMemory();
            }

            return value;
        }

        public ReadOnlyMemory<char> sameCharEscaper(char escapeStringDelimiterChar, char stringDelimiterChar, ReadOnlyMemory<char> stringValue)
        {
            var value = stringValue;
            int i = 1;
            bool substitutionHappened = false;
            bool escaping = false;
            string r = string.Empty;
            while (i < value.Length - 1)
            {
                char current = value.At(i);
                if (current == escapeStringDelimiterChar && !escaping && i < value.Length - 2)
                {
                    escaping = true;
                    if (!substitutionHappened)
                    {
                        r = value.Slice(0, i).ToString();
                        substitutionHappened = true;
                    }
                }
                else
                {
                    if (escaping)
                    {
                        r += escapeStringDelimiterChar;
                        escaping = false;
                    }
                    else if (substitutionHappened)
                    {
                        r += current;
                    }
                }
                i++;
            }
            if (substitutionHappened)
            {
                r += value.At(value.Length - 1);
                value = r.AsMemory();
            }

            return value;
        }

        public void AddStringLexem(TLexeme token, string stringDelimiter, string escapeDelimiterChar = "\\")
        {
            if (string.IsNullOrEmpty(stringDelimiter) || stringDelimiter.Length > 1)
                throw new InvalidLexerException(
                    $"bad lexem {stringDelimiter} :  StringToken lexeme delimiter char <{token.ToString()}> must be 1 character length.");
            if (char.IsLetterOrDigit(stringDelimiter[0]))
                throw new InvalidLexerException(
                    $"bad lexem {stringDelimiter} :  StringToken lexeme delimiter char <{token.ToString()}> can not start with a letter.");

            if (string.IsNullOrEmpty(escapeDelimiterChar) || escapeDelimiterChar.Length > 1)
                throw new InvalidLexerException(
                    $"bad lexem {escapeDelimiterChar} :  StringToken lexeme escape char  <{token.ToString()}> must be 1 character length.");
            if (char.IsLetterOrDigit(escapeDelimiterChar[0]))
                throw new InvalidLexerException(
                    $"bad lexem {escapeDelimiterChar} :  StringToken lexeme escape char lexeme <{token.ToString()}> can not start with a letter.");

            StringCounter++;

            StringDelimiterChar = stringDelimiter[0];
            var stringDelimiterChar = stringDelimiter[0];

            EscapeStringDelimiterChar = escapeDelimiterChar[0];
            var escapeStringDelimiterChar = escapeDelimiterChar[0];



            NodeCallback<GenericToken> callback = match =>
            {
                match.Properties[DerivedToken] = token;
                var value = match.Result.SpanValue;

                match.Result.SpanValue = value;


                if (stringDelimiterChar != escapeStringDelimiterChar)
                {
                    match.Result.SpanValue = diffCharEscaper(escapeStringDelimiterChar,stringDelimiterChar, match.Result.SpanValue);
                }
                else
                {
                    match.Result.SpanValue = sameCharEscaper(escapeStringDelimiterChar,stringDelimiterChar, match.Result.SpanValue);
                }

                return match;
            };

            if (stringDelimiterChar != escapeStringDelimiterChar)
            {

                FSMBuilder.GoTo(start);
                FSMBuilder.Transition(stringDelimiterChar)
                    .Mark(in_string + StringCounter)
                    .ExceptTransitionTo(new[] { stringDelimiterChar, escapeStringDelimiterChar },
                        in_string + StringCounter)
                    .Transition(escapeStringDelimiterChar)
                    .Mark(escape_string + StringCounter)
                    .ExceptTransitionTo(new[] { stringDelimiterChar }, in_string + StringCounter)
                    .GoTo(escape_string + StringCounter)
                    .TransitionTo(stringDelimiterChar, in_string + StringCounter)
                    .Transition(stringDelimiterChar)
                    .End(GenericToken.String)
                    .Mark(string_end + StringCounter)
                    .CallBack(callback);
                FSMBuilder.Fsm.StringDelimiter = stringDelimiterChar;
            }
            else
            {
                var exceptDelimiter = new[] { stringDelimiterChar };
                var in_string = "in_string_same";
                var escaped = "escaped_same";
                var delim = "delim_same";

                FSMBuilder.GoTo(start)
                    .Transition(stringDelimiterChar)
                    .Mark(in_string + StringCounter)
                    .ExceptTransitionTo(exceptDelimiter, in_string + StringCounter)
                    .Transition(stringDelimiterChar)
                    .Mark(escaped + StringCounter)
                    .End(GenericToken.String)
                    .CallBack(callback)
                    .Transition(stringDelimiterChar)
                    .Mark(delim + StringCounter)
                    .ExceptTransitionTo(exceptDelimiter, in_string + StringCounter);

                FSMBuilder.GoTo(delim + StringCounter)
                    .TransitionTo(stringDelimiterChar, escaped + StringCounter)
                    .ExceptTransitionTo(exceptDelimiter, in_string + StringCounter);
            }
        }

        public void AddCharLexem(TLexeme token, string charDelimiter, string escapeDelimiterChar = "\\")
        {
            if (string.IsNullOrEmpty(charDelimiter) || charDelimiter.Length > 1)
                throw new InvalidLexerException(
                    $"bad lexem {charDelimiter} :  CharToken lexeme delimiter char <{token.ToString()}> must be 1 character length.");
            if (char.IsLetterOrDigit(charDelimiter[0]))
                throw new InvalidLexerException(
                    $"bad lexem {charDelimiter} :  CharToken lexeme delimiter char <{token.ToString()}> can not start with a letter.");

            if (string.IsNullOrEmpty(escapeDelimiterChar) || escapeDelimiterChar.Length > 1)
                throw new InvalidLexerException(
                    $"bad lexem {escapeDelimiterChar} :  CharToken lexeme escape char  <{token.ToString()}> must be 1 character length.");
            if (char.IsLetterOrDigit(escapeDelimiterChar[0]))
                throw new InvalidLexerException(
                    $"bad lexem {escapeDelimiterChar} :  CharToken lexeme escape char lexeme <{token.ToString()}> can not start with a letter.");

            CharCounter++;

            var charDelimiterChar = charDelimiter[0];

            var escapeChar = escapeDelimiterChar[0];


            NodeCallback<GenericToken> callback = match =>
            {
                match.Properties[DerivedToken] = token;
                var value = match.Result.SpanValue;

                match.Result.SpanValue = value;
                return match;
            };

            FSMBuilder.GoTo(start);
            FSMBuilder.Transition(charDelimiterChar)
                .Mark(start_char+"_"+CharCounter)
                .ExceptTransition(new[] { charDelimiterChar, escapeChar })
                .Mark(in_char+"_"+CharCounter)
                .Transition(charDelimiterChar)
                .Mark(end_char+"_"+CharCounter)
                .End(GenericToken.Char)
                .CallBack(callback)
                .GoTo(start_char+"_"+CharCounter)
                .Transition(escapeChar)
                .Mark(escapeChar_char+"_"+CharCounter)
                .ExceptTransitionTo(new[] { 'u' }, in_char + "_" + CharCounter)
                .CallBack(callback);
            FSMBuilder.Fsm.StringDelimiter = charDelimiterChar;

            // TODO : unicode transitions
            FSMBuilder = FSMBuilder.GoTo(escapeChar_char + "_" + CharCounter)
            .Transition('u')
            .Mark(unicode_char+"_"+CharCounter)
            .RepetitionTransitionTo(in_char + "_" + CharCounter,4,"[0-9,a-z,A-Z]");

        }

        public void AddSugarLexem(TLexeme token, string specialValue)
        {
            if (char.IsLetter(specialValue[0]))
                throw new InvalidLexerException(
                    $"bad lexem {specialValue} :  SugarToken lexeme <{token.ToString()}>  can not start with a letter.");
            NodeCallback<GenericToken> callback = match =>
            {
                match.Properties[DerivedToken] = token;
                return match;
            };

            FSMBuilder.GoTo(start);
            for (var i = 0; i < specialValue.Length; i++) FSMBuilder.SafeTransition(specialValue[i]);
            FSMBuilder.End(GenericToken.SugarToken)
                .CallBack(callback);
        }

        public void ConsumeComment(Token<GenericToken> comment, ReadOnlyMemory<char> source)
        {

            ReadOnlyMemory<char> commentValue;

            if (comment.IsSingleLineComment)
            {
                var position = LexerFsm.CurrentPosition;
                commentValue = EOLManager.GetToEndOfLine(source, position);
                position = position + commentValue.Length;
                comment.SpanValue = commentValue;
                LexerFsm.MovePosition(position, LexerFsm.CurrentLine + 1, 0);
            }
            else if (comment.IsMultiLineComment)
            {
                var position = LexerFsm.CurrentPosition;

                var end = source.Span.Slice(position).IndexOf(MultiLineCommentEnd.AsSpan());
                if (end < 0)
                    position = source.Length;
                else
                    position = end + position;
                commentValue = source.Slice(LexerFsm.CurrentPosition, position - LexerFsm.CurrentPosition);
                comment.SpanValue = commentValue;

                var newPosition = LexerFsm.CurrentPosition + commentValue.Length + MultiLineCommentEnd.Length;
                var lines = EOLManager.GetLinesLength(commentValue);
                var newLine = LexerFsm.CurrentLine + lines.Count - 1;
                int newColumn;
                if (lines.Count > 1)
                    newColumn = lines.Last() + MultiLineCommentEnd.Length;
                else
                    newColumn = LexerFsm.CurrentColumn + lines[0] + MultiLineCommentEnd.Length;


                LexerFsm.MovePosition(newPosition, newLine, newColumn);
            }
        }

        public Token<TLexeme> Transcode(FSMMatch<GenericToken> match)
        {
            var tok = new Token<TLexeme>();
            var inTok = match.Result;
            tok.IsComment = inTok.IsComment;
            tok.IsEmpty = inTok.IsEmpty;
            tok.SpanValue = inTok.SpanValue;
            tok.CommentType = inTok.CommentType;
            tok.Position = inTok.Position;
            tok.Discarded = inTok.Discarded;
            tok.StringDelimiter = StringDelimiterChar;
            tok.TokenID = (TLexeme)match.Properties[DerivedToken];
            return tok;
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return LexerFsm.ToString();
        }
        
        public string ToGraphViz()
        {
            return LexerFsm.ToGraphViz();
        }
        
        public void ResetLexer()
        {
            LexerFsm.Reset();
        }
    }
}