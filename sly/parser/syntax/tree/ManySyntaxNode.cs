using System.Collections.Generic;

namespace sly.parser.syntax.tree
{
    public class ManySyntaxNode<TIn> : SyntaxNode<TIn> where TIn : struct
    {
        public ManySyntaxNode(string name) : base(name, new List<ISyntaxNode<TIn>>())
        {
        }

        public bool IsManyTokens { get; set; }

        public bool IsManyValues { get; set; }

        public bool IsManyGroups { get; set; }


        public void Add(ISyntaxNode<TIn> child)
        {
            Children.Add(child);
        }
    }
}