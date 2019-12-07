using csly.whileLang.compiler;

namespace csly.whileLang.model
{
    public interface IExpression : IWhileAst
    {
        WhileType Whiletype { get; set; }
    }
}