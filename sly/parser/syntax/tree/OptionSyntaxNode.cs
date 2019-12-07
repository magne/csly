using System.Collections.Generic;
using System.Reflection;

namespace sly.parser.syntax.tree
{
    public class OptionSyntaxNode<TIn> : SyntaxNode<TIn> where TIn : struct
    {
        public bool IsGroupOption { get; set; }

        public OptionSyntaxNode(string name, List<ISyntaxNode<TIn>> children = null, MethodInfo visitor = null) : base(
            name, children, visitor)
        { }
    }
}