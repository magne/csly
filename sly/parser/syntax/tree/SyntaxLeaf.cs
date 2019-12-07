using sly.lexer;

namespace sly.parser.syntax.tree
{
    public class SyntaxLeaf<TIn> : ISyntaxNode<TIn> where TIn : struct
    {
        public SyntaxLeaf(Token<TIn> token, bool discarded)
        {
            Token = token;
            Discarded = discarded;
        }

        public Token<TIn> Token { get; }
        public bool Discarded { get; }
        public string Name => Token.TokenID.ToString();
    }
}