using System;
// ReSharper disable InconsistentNaming

namespace sly.v3.lexer
{
    [AttributeUsage(AttributeTargets.Enum)]
    internal class LexerAttribute : Attribute
    {
        private static readonly GenericLexer<int>.Config Defaults = new GenericLexer<int>.Config();

        private bool? ignoreWS;

        private bool? ignoreEOL;

        private char[] whiteSpace;

        private bool? keyWordIgnoreCase;

        public bool IgnoreWS
        {
            get => ignoreWS ?? Defaults.IgnoreWS;
            set => ignoreWS = value;
        }

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