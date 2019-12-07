using System;

namespace sly.lexer
{
    [AttributeUsage(AttributeTargets.Enum)]
    public class LexerAttribute : Attribute
    {
        private static readonly GenericLexer<int>.Config Defaults = new GenericLexer<int>.Config();

        // ReSharper disable once InconsistentNaming
        private bool? ignoreWS;

        // ReSharper disable once InconsistentNaming
        private bool? ignoreEOL;

        private char[] whiteSpace;

        private bool? keyWordIgnoreCase;

        // ReSharper disable once InconsistentNaming
        public bool IgnoreWS
        {
            get => ignoreWS ?? Defaults.IgnoreWS;
            set => ignoreWS = value;
        }

        // ReSharper disable once InconsistentNaming
        public bool IgnoreEOL
        {
            get => ignoreEOL ?? Defaults.IgnoreEOL;
            set => ignoreEOL = value;
        }

        public char[] WhiteSpace
        {
            get => whiteSpace ?? Defaults.WhiteSpace;
            set => whiteSpace = value;
        }

        public bool KeyWordIgnoreCase
        {
            get => keyWordIgnoreCase ?? Defaults.KeyWordIgnoreCase;
            set => keyWordIgnoreCase = value;
        }
    }
}