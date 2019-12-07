using System;
using System.Diagnostics.CodeAnalysis;
using sly.lexer;

namespace sly.parser.parser
{
    public class GroupItem<TIn, TOut>
    {
        public string Name;

        public GroupItem(string name)
        {
            Name = name;
            IsToken = false;
            IsValue = false;
        }

        public GroupItem(string name, Token<TIn> token)
        {
            Name = name;
            IsToken = true;
            Token = token;
        }

        public GroupItem(string name, TOut value)
        {
            Name = name;
            IsValue = true;
            Value = value;
        }

        public Token<TIn> Token { get; }

        public bool IsToken { get; set; }

        public TOut Value { get; set; }

        public bool IsValue { get; }


        public TX Match<TX>(Func<string, Token<TIn>, TX> fToken, Func<string, TOut, TX> fValue)
        {
            if (IsToken)
                return fToken(Name, Token);
            return fValue(Name, Value);
        }

        public static implicit operator TOut(GroupItem<TIn, TOut> item)
        {
            return item.Match((name, token) => default(TOut), (name, value) => item.Value);
        }

        public static implicit operator Token<TIn>(GroupItem<TIn, TOut> item)
        {
            return item.Match((name, token) => item.Token, (name, value) => default(Token<TIn>));
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return IsValue ? ((TOut) this).ToString() : ((Token<TIn>) this).Value;
        }
    }
}