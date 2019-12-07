using System;
using System.Collections.Generic;
using System.Reflection;
using sly.lexer;
using sly.parser.parser;
using sly.parser.syntax.tree;
using static sly.parser.parser.ValueOptionConstructors;

namespace sly.parser.generator.visitor
{
    public class SyntaxVisitorResult<TIn, TOut> where TIn : struct
    {
        public List<Group<TIn, TOut>> GroupListResult;

        public Group<TIn, TOut> GroupResult;

        public ValueOption<Group<TIn, TOut>> OptionGroupResult;

        public ValueOption<TOut> OptionResult;

        public List<Token<TIn>> TokenListResult;

        public Token<TIn> TokenResult;

        public List<TOut> ValueListResult;

        public TOut ValueResult;

        public bool IsOption => OptionResult != null;
        public bool IsOptionGroup => OptionGroupResult != null;

        public bool IsToken { get; private set; }

        public bool Discarded => IsToken && TokenResult != null && TokenResult.Discarded;
        public bool IsValue { get; private set; }
        public bool IsValueList { get; private set; }

        public bool IsGroupList { get; private set; }

        public bool IsTokenList { get; private set; }

        public bool IsGroup { get; private set; }

        public bool IsNone => !IsToken && !IsValue && !IsTokenList && !IsValueList && !IsGroup && !IsGroupList;

        public static SyntaxVisitorResult<TIn, TOut> NewToken(Token<TIn> tok)
        {
            var res = new SyntaxVisitorResult<TIn, TOut>();
            res.TokenResult = tok;
            res.IsToken = true;
            return res;
        }

        public static SyntaxVisitorResult<TIn, TOut> NewValue(TOut val)
        {
            var res = new SyntaxVisitorResult<TIn, TOut>();
            res.ValueResult = val;
            res.IsValue = true;
            return res;
        }

        public static SyntaxVisitorResult<TIn, TOut> NewValueList(List<TOut> values)
        {
            var res = new SyntaxVisitorResult<TIn, TOut>();
            res.ValueListResult = values;
            res.IsValueList = true;
            return res;
        }

        public static SyntaxVisitorResult<TIn, TOut> NewGroupList(List<Group<TIn, TOut>> values)
        {
            var res = new SyntaxVisitorResult<TIn, TOut>();
            res.GroupListResult = values;
            res.IsGroupList = true;
            return res;
        }

        public static SyntaxVisitorResult<TIn, TOut> NewTokenList(List<Token<TIn>> tokens)
        {
            var res = new SyntaxVisitorResult<TIn, TOut>();
            res.TokenListResult = tokens;
            res.IsTokenList = true;
            return res;
        }

        public static SyntaxVisitorResult<TIn, TOut> NewOptionSome(TOut value)
        {
            var res = new SyntaxVisitorResult<TIn, TOut>();
            res.OptionResult = Some(value);
            return res;
        }

        public static SyntaxVisitorResult<TIn, TOut> NewOptionGroupSome(Group<TIn, TOut> group)
        {
            var res = new SyntaxVisitorResult<TIn, TOut>();
            res.OptionGroupResult = Some(group);
            return res;
        }

        public static SyntaxVisitorResult<TIn, TOut> NewOptionGroupNone()
        {
            var res = new SyntaxVisitorResult<TIn, TOut>();
            res.OptionGroupResult = NoneGroup<TIn, TOut>();
            return res;
        }


        public static SyntaxVisitorResult<TIn, TOut> NewOptionNone()
        {
            var res = new SyntaxVisitorResult<TIn, TOut>();
            res.OptionResult = None<TOut>();
            return res;
        }

        public static SyntaxVisitorResult<TIn, TOut> NewGroup(Group<TIn, TOut> group)
        {
            var res = new SyntaxVisitorResult<TIn, TOut>();
            res.GroupResult = group;
            res.IsGroup = true;
            return res;
        }

        public static SyntaxVisitorResult<TIn, TOut> NoneResult()
        {
            var res = new SyntaxVisitorResult<TIn, TOut>();
            return res;
        }
    }

    public class SyntaxTreeVisitor<TIn, TOut> where TIn : struct
    {
        public SyntaxTreeVisitor(ParserConfiguration<TIn, TOut> conf, object parserInstance)
        {
            ParserClass = ParserClass;
            Configuration = conf;
            ParserVsisitorInstance = parserInstance;
        }

        public Type ParserClass { get; set; }

        public object ParserVsisitorInstance { get; set; }

        public ParserConfiguration<TIn, TOut> Configuration { get; set; }

        public TOut VisitSyntaxTree(ISyntaxNode<TIn> root, object context = null)
        {
            var result = Visit(root, context);
            return result.ValueResult;
        }

        protected virtual SyntaxVisitorResult<TIn, TOut> Visit(ISyntaxNode<TIn> n, object context = null)
        {
            if (n is SyntaxLeaf<TIn>)
                return Visit(n as SyntaxLeaf<TIn>);
            if (n is SyntaxNode<TIn>)
                return Visit(n as SyntaxNode<TIn>, context);
            return null;
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
                        if (!v.Discarded) args.Add(v.TokenResult);
                    }
                    else if (v.IsValue)
                    {
                        args.Add(v.ValueResult);
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
                        if (!(context is NoContext))
                        {
                            args.Add(context);
                        }

                        method = node.Visitor;
                        var t = method?.Invoke(ParserVsisitorInstance, args.ToArray());
                        var res = (TOut) t;
                        result = SyntaxVisitorResult<TIn, TOut>.NewValue(res);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"ERROR : {e.Message} calling {node.Name} =>  {method?.Name}");
                    }
                }
            }

            return result;
        }

        private SyntaxVisitorResult<TIn, TOut> Visit(SyntaxLeaf<TIn> leaf)
        {
            return SyntaxVisitorResult<TIn, TOut>.NewToken(leaf.Token);
        }
    }
}