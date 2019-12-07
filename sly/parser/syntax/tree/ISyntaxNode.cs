namespace sly.parser.syntax.tree
{
    public interface ISyntaxNode<TIn> where TIn : struct
    {
        bool Discarded { get; }
        string Name { get; }
    }
}