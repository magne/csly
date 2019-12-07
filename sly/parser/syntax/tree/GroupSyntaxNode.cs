using System.Collections.Generic;

namespace sly.parser.syntax.tree
{
    public class GroupSyntaxNode<TIn> : ManySyntaxNode<TIn> where TIn : struct
    {
        public GroupSyntaxNode(string name) : base(name)
        {
        }

        public GroupSyntaxNode(string name,  List<ISyntaxNode<TIn>> children) : this(name)
        {
            Children.AddRange(children);
        }

    }
}