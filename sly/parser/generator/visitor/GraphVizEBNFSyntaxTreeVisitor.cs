using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using sly.parser.generator.visitor.dotgraph;
using sly.parser.syntax.tree;

namespace sly.parser.generator.visitor
{
    [ExcludeFromCodeCoverage]
    // ReSharper disable once InconsistentNaming
    public class GraphVizEBNFSyntaxTreeVisitor<TIn> where TIn : struct
    {
        public DotGraph Graph { get; }

        public GraphVizEBNFSyntaxTreeVisitor()
        {
            Graph = new DotGraph("syntaxtree", true);
        }

        private int nodeCounter;


        private DotNode Leaf(SyntaxLeaf<TIn> leaf)
        {
            return Leaf(leaf.Token.TokenID, leaf.Token.Value);
        }

        private DotNode Leaf(TIn type, string value)
        {
            string label = type.ToString();
            label += "\n";
            var esc = value.Replace("\"", "\\\"");
            label += "\\\"" + esc + "\\\"";
            var node = new DotNode(nodeCounter.ToString())
            {
                // Set all available properties
                Shape = "doublecircle",
                Label = label,
                FontColor = "",
                Style = "",
                Height = 0.5f
            };
            nodeCounter++;
            Graph.Add(node);
            return node;
        }

        public DotNode VisitTree(ISyntaxNode<TIn> root)
        {
            return Visit(root);
        }

        private DotNode Node(string label)
        {
            var node = new DotNode(nodeCounter.ToString())
            {
                // Set all available properties
                Shape = "ellipse",
                Label = label,
                FontColor = "black",
                Style = null,
                Height = 0.5f
            };
            nodeCounter++;
            Graph.Add(node);
            return node;
        }

        protected DotNode Visit(ISyntaxNode<TIn> n)
        {
            if (n is SyntaxLeaf<TIn>)
                return Visit(n as SyntaxLeaf<TIn>);
            if (n is GroupSyntaxNode<TIn>)
                return Visit(n as GroupSyntaxNode<TIn>);
            if (n is ManySyntaxNode<TIn>)
                return Visit(n as ManySyntaxNode<TIn>);
            if (n is OptionSyntaxNode<TIn>)
                return Visit(n as OptionSyntaxNode<TIn>);
            if (n is SyntaxNode<TIn>)
                return Visit(n as SyntaxNode<TIn>);

            return null;
        }

        private DotNode Visit(GroupSyntaxNode<TIn> node)
        {
            return Visit(node as SyntaxNode<TIn>);
        }

        private DotNode Visit(OptionSyntaxNode<TIn> node)
        {
            var child = node.Children != null && node.Children.Any() ? node.Children[0] : null;
            if (child == null || node.IsEmpty)
            {
                return null;
            }

            return Visit(child);
        }

        private string GetNodeLabel(SyntaxNode<TIn> node)
        {
            string label = node.Name;
            if (node.IsExpressionNode)
            {
                label = node.Operation.OperatorToken.ToString();
            }

            return label;
        }

        private DotNode Visit(SyntaxNode<TIn> node)
        {
            DotNode result;


            var children = new List<DotNode>();

            foreach (var n in node.Children)
            {
                var v = Visit(n);

                children.Add(v);
            }

            if (node.IsByPassNode)
            {
                result = children[0];
            }
            else
            {
                result = Node(GetNodeLabel(node));
                Graph.Add(result);
                children.ForEach(c =>
                {
                    if (c != null) // Prevent arrows with null destinations
                    {
                        var edge = new DotArrow(result, c)
                        {
                            // Set all available properties
                            ArrowHeadShape = "none"
                        };
                        Graph.Add(edge);
                    }
                });
            }

            return result;
        }

        private DotNode Visit(ManySyntaxNode<TIn> node)
        {
            return Visit(node as SyntaxNode<TIn>);
        }


        private DotNode Visit(SyntaxLeaf<TIn> leaf)
        {
            return Leaf(leaf.Token.TokenID, leaf.Token.Value);
        }
    }
}