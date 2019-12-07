using System.Collections.Generic;
using sly.parser.syntax.tree;

namespace sly.parser.parser
{
    public class SyntaxTreeCleaner<TIn> where TIn : struct
    {
        public SyntaxParseResult<TIn> CleanSyntaxTree(SyntaxParseResult<TIn> result)
        {
            var tree = result.Root;
            if (tree != null)
            {
                tree = RemoveByPassNodes(tree);
                if (NeedAssociativityProcessing(tree)) tree = SetAssociativity(tree);
                result.Root = tree;
            }

            return result;
        }

        private bool NeedAssociativityProcessing(ISyntaxNode<TIn> tree)
        {
            var need = false;
            if (tree is ManySyntaxNode<TIn> many)
                foreach (var child in many.Children)
                {
                    need = need || NeedAssociativityProcessing(child);
                    if (need) break;
                }
            else if (tree is SyntaxLeaf<TIn> leaf)
                need = false;
            else if (tree is SyntaxNode<TIn> node) need = node.IsExpressionNode;

            return need;
        }

        private ISyntaxNode<TIn> RemoveByPassNodes(ISyntaxNode<TIn> tree)
        {
            ISyntaxNode<TIn> result = null;


            if (tree is SyntaxNode<TIn> node && node.IsByPassNode)
            {
                result = RemoveByPassNodes(node.Children[0]);
            }
            else
            {
                if (tree is SyntaxLeaf<TIn> leaf) result = leaf;
                if (tree is SyntaxNode<TIn> innernode)
                {
                    var newChildren = new List<ISyntaxNode<TIn>>();
                    foreach (var child in innernode.Children) newChildren.Add(RemoveByPassNodes(child));
                    innernode.Children.Clear();
                    innernode.Children.AddRange(newChildren);
                    result = innernode;
                }

                if (tree is ManySyntaxNode<TIn> many)
                {
                    var newChildren = new List<ISyntaxNode<TIn>>();
                    foreach (var child in many.Children) newChildren.Add(RemoveByPassNodes(child));
                    many.Children.Clear();
                    many.Children.AddRange(newChildren);
                    result = many;
                }
            }

            return result;
        }


        private ISyntaxNode<TIn> SetAssociativity(ISyntaxNode<TIn> tree)
        {
            ISyntaxNode<TIn> result = null;


            if (tree is ManySyntaxNode<TIn> many)
            {
                var newChildren = new List<ISyntaxNode<TIn>>();
                foreach (var child in many.Children) newChildren.Add(SetAssociativity(child));
                many.Children.Clear();
                many.Children.AddRange(newChildren);
                result = many;
            }
            else if (tree is SyntaxLeaf<TIn> leaf)
            {
                result = leaf;
            }
            else if (tree is SyntaxNode<TIn> node)
            {
                if (NeedLeftAssociativity(node)) node = ProcessLeftAssociativity(node);
                var newChildren = new List<ISyntaxNode<TIn>>();
                foreach (var child in node.Children) newChildren.Add(SetAssociativity(child));
                node.Children.Clear();
                node.Children.AddRange(newChildren);
                result = node;
            }

            return result;
        }


        private bool NeedLeftAssociativity(SyntaxNode<TIn> node)
        {
            return node.IsBinaryOperationNode && node.IsLeftAssociative
                                              && node.Right is SyntaxNode<TIn> right && right.IsExpressionNode
                                              && right.Precedence == node.Precedence;
        }

        private SyntaxNode<TIn> ProcessLeftAssociativity(SyntaxNode<TIn> node)
        {
            var result = node;
            while (NeedLeftAssociativity(result))
            {
                var newLeft = result;
                var newTop = (SyntaxNode<TIn>) result.Right;
                newLeft.Children[2] = newTop.Left;
                newTop.Children[0] = newLeft;
                result = newTop;
            }

            return result;
        }
    }
}