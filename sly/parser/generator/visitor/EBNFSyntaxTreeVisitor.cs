using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using sly.lexer;
using sly.parser.parser;
using sly.parser.syntax.tree;

namespace sly.parser.generator.visitor
{
    // ReSharper disable once InconsistentNaming
    public class EBNFSyntaxTreeVisitor<TIn, TOut> : SyntaxTreeVisitor<TIn, TOut> where TIn : struct
    {
        public EBNFSyntaxTreeVisitor(ParserConfiguration<TIn, TOut> conf, object parserInstance) : base(conf,
            parserInstance)
        { }


        protected override SyntaxVisitorResult<TIn, TOut> Visit(ISyntaxNode<TIn> n, object context = null)
        {
            if (n is SyntaxLeaf<TIn>)
                return Visit(n as SyntaxLeaf<TIn>);
            if (n is GroupSyntaxNode<TIn>)
                return Visit(n as GroupSyntaxNode<TIn>, context);
            if (n is ManySyntaxNode<TIn>)
                return Visit(n as ManySyntaxNode<TIn>, context);
            if (n is OptionSyntaxNode<TIn>)
                return Visit(n as OptionSyntaxNode<TIn>, context);
            if (n is SyntaxNode<TIn>)
                return Visit(n as SyntaxNode<TIn>, context);

            return null;
        }

        private SyntaxVisitorResult<TIn, TOut> Visit(GroupSyntaxNode<TIn> node, object context = null)
        {
            var group = new Group<TIn, TOut>();
            foreach (var n in node.Children)
            {
                var v = Visit(n, context);

                if (v.IsValue) group.Add(n.Name, v.ValueResult);
                if (v.IsToken)
                    if (!v.Discarded)
                        group.Add(n.Name, v.TokenResult);
            }


            var res = SyntaxVisitorResult<TIn, TOut>.NewGroup(group);
            return res;
        }

        private SyntaxVisitorResult<TIn, TOut> Visit(OptionSyntaxNode<TIn> node, object context = null)
        {
            var child = node.Children != null && node.Children.Any() ? node.Children[0] : null;
            if (child == null || node.IsEmpty)
            {
                if (node.IsGroupOption)
                {
                    return SyntaxVisitorResult<TIn, TOut>.NewOptionGroupNone();
                }
                else
                {
                    return SyntaxVisitorResult<TIn, TOut>.NewOptionNone();
                }
            }

            var innerResult = Visit(child, context);
            if (child is SyntaxLeaf<TIn> leaf)
                return SyntaxVisitorResult<TIn, TOut>.NewToken(leaf.Token);
            if (child is GroupSyntaxNode<TIn>)
                return SyntaxVisitorResult<TIn, TOut>.NewOptionGroupSome(innerResult.GroupResult);
            return SyntaxVisitorResult<TIn, TOut>.NewOptionSome(innerResult.ValueResult);
        }


        private SyntaxVisitorResult<TIn, TOut> Visit(SyntaxNode<TIn> node, object context = null)
        {
            var result = SyntaxVisitorResult<TIn, TOut>.NoneResult();
            if (node.Visitor != null || node.IsByPassNode)
            {
                var args = new List<object>();

                foreach (var n in node.Children)
                {
                    var v = Visit(n, context);

                    if (v.IsToken)
                    {
                        if (!n.Discarded) args.Add(v.TokenResult);
                    }
                    else if (v.IsValue)
                    {
                        args.Add(v.ValueResult);
                    }
                    else if (v.IsOption)
                    {
                        args.Add(v.OptionResult);
                    }
                    else if (v.IsOptionGroup)
                    {
                        args.Add(v.OptionGroupResult);
                    }
                    else if (v.IsGroup)
                    {
                        args.Add(v.GroupResult);
                    }
                    else if (v.IsTokenList)
                    {
                        args.Add(v.TokenListResult);
                    }
                    else if (v.IsValueList)
                    {
                        args.Add(v.ValueListResult);
                    }
                    else if (v.IsGroupList)
                    {
                        args.Add(v.GroupListResult);
                    }
                }

                if (node.IsByPassNode)
                {
                    result = SyntaxVisitorResult<TIn, TOut>.NewValue((TOut) args[0]);
                }
                else
                {
                    MethodInfo method = null;
                    try
                    {
                        if (method == null) method = node.Visitor;
                        var t = method.Invoke(ParserVsisitorInstance, args.ToArray());
                        var res = (TOut) t;
                        result = SyntaxVisitorResult<TIn, TOut>.NewValue(res);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OUTCH {e.Message} calling {node.Name} =>  {method.Name}");
                    }
                }
            }

            return result;
        }

        private SyntaxVisitorResult<TIn, TOut> Visit(ManySyntaxNode<TIn> node, object context = null)
        {
            SyntaxVisitorResult<TIn, TOut> result = null;

            var values = new List<SyntaxVisitorResult<TIn, TOut>>();
            foreach (var n in node.Children)
            {
                var v = Visit(n, context);
                values.Add(v);
            }

            if (node.IsManyTokens)
            {
                var tokens = new List<Token<TIn>>();
                values.ForEach(v => tokens.Add(v.TokenResult));
                result = SyntaxVisitorResult<TIn, TOut>.NewTokenList(tokens);
            }
            else if (node.IsManyValues)
            {
                var vals = new List<TOut>();
                values.ForEach(v => vals.Add(v.ValueResult));
                result = SyntaxVisitorResult<TIn, TOut>.NewValueList(vals);
            }
            else if (node.IsManyGroups)
            {
                var vals = new List<Group<TIn, TOut>>();
                values.ForEach(v => vals.Add(v.GroupResult));
                result = SyntaxVisitorResult<TIn, TOut>.NewGroupList(vals);
            }


            return result;
        }


        private SyntaxVisitorResult<TIn, TOut> Visit(SyntaxLeaf<TIn> leaf)
        {
            return SyntaxVisitorResult<TIn, TOut>.NewToken(leaf.Token);
        }
    }
}