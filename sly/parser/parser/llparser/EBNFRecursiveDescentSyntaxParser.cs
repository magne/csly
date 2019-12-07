using System;
using System.Collections.Generic;
using System.Linq;
using sly.lexer;
using sly.parser.generator;
using sly.parser.syntax.grammar;
using sly.parser.syntax.tree;

namespace sly.parser.llparser
{
    // ReSharper disable once InconsistentNaming
    public class EBNFRecursiveDescentSyntaxParser<TIn, TOut> : RecursiveDescentSyntaxParser<TIn, TOut> where TIn : struct
    {
        public EBNFRecursiveDescentSyntaxParser(ParserConfiguration<TIn, TOut> configuration, string startingNonTerminal)
            : base(configuration, startingNonTerminal)
        { }

        #region STARTING_TOKENS

        protected override void InitStartingTokensForRule(Dictionary<string, NonTerminal<TIn>> nonTerminals,
            Rule<TIn> rule)
        {
            if (rule.PossibleLeadingTokens == null || rule.PossibleLeadingTokens.Count == 0)
            {
                rule.PossibleLeadingTokens = new List<TIn>();
                if (rule.Clauses.Count > 0)
                {
                    var first = rule.Clauses[0];
                    if (first is TerminalClause<TIn>)
                    {
                        var term = first as TerminalClause<TIn>;

                        InitStartingTokensWithTerminal(rule, term);
                    }
                    else if (first is NonTerminalClause<TIn>)
                    {
                        var nonterm = first as NonTerminalClause<TIn>;
                        InitStartingTokensWithNonTerminal(rule, nonterm, nonTerminals);
                    }
                    else if (first is ZeroOrMoreClause<TIn>)
                    {
                        var many = first as ZeroOrMoreClause<TIn>;
                        InitStartingTokensWithZeroOrMore(rule, many, nonTerminals);
                        int i = 1;
                        bool optional = first is ZeroOrMoreClause<TIn> || first is OptionClause<TIn>;
                        while (i < rule.Clauses.Count && optional)
                        {
                            IClause<TIn> clause = rule.Clauses[i];

                            switch (clause)
                            {
                                case TerminalClause<TIn> terminalClause:
                                {
                                    rule.PossibleLeadingTokens.Add(terminalClause.ExpectedToken);
                                    break;
                                }
                                case NonTerminalClause<TIn> terminalClause:
                                {
                                    InitStartingTokensForNonTerminal(nonTerminals, terminalClause.NonTerminalName);
                                    NonTerminal<TIn> nonTerminal = nonTerminals[terminalClause.NonTerminalName];
                                    {
                                        rule.PossibleLeadingTokens.AddRange(nonTerminal.PossibleLeadingTokens);
                                    }
                                    break;
                                }
                            }

                            // add startig tokens of clause in rule.startingtokens

                            optional = clause is ZeroOrMoreClause<TIn> || clause is OptionClause<TIn>;
                            i++;
                        }
                    }
                    else if (first is OneOrMoreClause<TIn>)
                    {
                        var many = first as OneOrMoreClause<TIn>;
                        InitStartingTokensWithOneOrMore(rule, many, nonTerminals);
                    }
                }
            }
        }


        private void InitStartingTokensWithTerminal(Rule<TIn> rule, TerminalClause<TIn> term)
        {
            rule.PossibleLeadingTokens.Add(term.ExpectedToken);
            rule.PossibleLeadingTokens = rule.PossibleLeadingTokens.Distinct().ToList();
        }

        private void InitStartingTokensWithNonTerminal(Rule<TIn> rule, NonTerminalClause<TIn> nonterm,
            Dictionary<string, NonTerminal<TIn>> nonTerminals)
        {
            InitStartingTokensForNonTerminal(nonTerminals, nonterm.NonTerminalName);
            if (nonTerminals.ContainsKey(nonterm.NonTerminalName))
            {
                var firstNonTerminal = nonTerminals[nonterm.NonTerminalName];
                firstNonTerminal.Rules.ForEach(r => { rule.PossibleLeadingTokens.AddRange(r.PossibleLeadingTokens); });
                rule.PossibleLeadingTokens = rule.PossibleLeadingTokens.Distinct().ToList();
            }
        }

        private void InitStartingTokensWithZeroOrMore(Rule<TIn> rule, ZeroOrMoreClause<TIn> manyClause,
            Dictionary<string, NonTerminal<TIn>> nonTerminals)
        {
            if (manyClause.Clause is TerminalClause<TIn>)
            {
                var term = manyClause.Clause as TerminalClause<TIn>;

                InitStartingTokensWithTerminal(rule, term);
            }
            else if (manyClause.Clause is NonTerminalClause<TIn>)
            {
                var nonterm = manyClause.Clause as NonTerminalClause<TIn>;
                InitStartingTokensWithNonTerminal(rule, nonterm, nonTerminals);
            }
        }

        private void InitStartingTokensWithOneOrMore(Rule<TIn> rule, OneOrMoreClause<TIn> manyClause,
            Dictionary<string, NonTerminal<TIn>> nonTerminals)
        {
            if (manyClause.Clause is TerminalClause<TIn>)
            {
                var term = manyClause.Clause as TerminalClause<TIn>;

                InitStartingTokensWithTerminal(rule, term);
            }
            else if (manyClause.Clause is NonTerminalClause<TIn>)
            {
                var nonterm = manyClause.Clause as NonTerminalClause<TIn>;
                InitStartingTokensWithNonTerminal(rule, nonterm, nonTerminals);
            }
        }

        #endregion

        #region parsing

        public override SyntaxParseResult<TIn> Parse(IList<Token<TIn>> tokens, Rule<TIn> rule, int position,
            string nonTerminalName)
        {
            var currentPosition = position;
            var errors = new List<UnexpectedTokenSyntaxError<TIn>>();
            var isError = false;
            var children = new List<ISyntaxNode<TIn>>();
            if (rule.PossibleLeadingTokens.Contains(tokens[position].TokenID) || rule.MayBeEmpty)
                if (rule.Clauses != null && rule.Clauses.Count > 0)
                {
                    children = new List<ISyntaxNode<TIn>>();
                    foreach (var clause in rule.Clauses)
                    {
                        if (clause is TerminalClause<TIn> termClause)
                        {
                            var termRes =
                                ParseTerminal(tokens, termClause, currentPosition);
                            if (!termRes.IsError)
                            {
                                children.Add(termRes.Root);
                                currentPosition = termRes.EndingPosition;
                            }
                            else
                            {
                                var tok = tokens[currentPosition];
                                errors.Add(new UnexpectedTokenSyntaxError<TIn>(tok,
                                    ((TerminalClause<TIn>) clause).ExpectedToken));
                            }

                            isError = termRes.IsError;
                        }
                        else if (clause is NonTerminalClause<TIn>)
                        {
                            var nonTerminalResult =
                                ParseNonTerminal(tokens, clause as NonTerminalClause<TIn>, currentPosition);
                            if (!nonTerminalResult.IsError)
                            {
                                errors.AddRange(nonTerminalResult.Errors);
                                children.Add(nonTerminalResult.Root);
                                currentPosition = nonTerminalResult.EndingPosition;
                            }
                            else
                            {
                                errors.AddRange(nonTerminalResult.Errors);
                            }

                            isError = nonTerminalResult.IsError;
                        }
                        else if (clause is OneOrMoreClause<TIn> || clause is ZeroOrMoreClause<TIn>)
                        {
                            SyntaxParseResult<TIn> manyResult = null;
                            if (clause is OneOrMoreClause<TIn> oneOrMore)
                                manyResult = ParseOneOrMore(tokens, oneOrMore, currentPosition);
                            else if (clause is ZeroOrMoreClause<TIn> zeroOrMore)
                                manyResult = ParseZeroOrMore(tokens, zeroOrMore, currentPosition);
                            if (!manyResult.IsError)
                            {
                                errors.AddRange(manyResult.Errors);
                                children.Add(manyResult.Root);
                                currentPosition = manyResult.EndingPosition;
                            }
                            else
                            {
                                if (manyResult.Errors != null && manyResult.Errors.Count > 0)
                                    errors.AddRange(manyResult.Errors);
                            }

                            isError = manyResult.IsError;
                        }
                        else if (clause is OptionClause<TIn> option)
                        {
                            var optionResult = ParseOption(tokens, option, rule, currentPosition);
                            currentPosition = optionResult.EndingPosition;
                            children.Add(optionResult.Root);
                        }

                        if (isError) break;
                    }
                }

            var result = new SyntaxParseResult<TIn>();
            result.IsError = isError;
            result.Errors = errors;
            result.EndingPosition = currentPosition;
            if (!isError)
            {
                SyntaxNode<TIn> node;
                if (rule.IsSubRule)
                {
                    node = new GroupSyntaxNode<TIn>(nonTerminalName, children);
                    node = ManageExpressionRules(rule, node);
                    result.Root = node;
                    result.IsEnded = currentPosition >= tokens.Count - 1
                                     || currentPosition == tokens.Count - 2 &&
                                     tokens[tokens.Count - 1].TokenID.Equals(default(TIn));
                }
                else
                {
                    node = new SyntaxNode<TIn>(nonTerminalName, children);
                    node.ExpressionAffix = rule.ExpressionAffix;
                    node = ManageExpressionRules(rule, node);
                    result.Root = node;
                    result.IsEnded = currentPosition >= tokens.Count - 1
                                     || currentPosition == tokens.Count - 2 &&
                                     tokens[tokens.Count - 1].TokenID.Equals(default(TIn));
                }
            }

            return result;
        }


        public SyntaxParseResult<TIn> ParseZeroOrMore(IList<Token<TIn>> tokens, ZeroOrMoreClause<TIn> clause, int position)
        {
            var result = new SyntaxParseResult<TIn>();
            var manyNode = new ManySyntaxNode<TIn>("");
            var currentPosition = position;
            var innerClause = clause.Clause;
            var stillOk = true;

            SyntaxParseResult<TIn> lastInnerResult = null;

            var innerErrors = new List<UnexpectedTokenSyntaxError<TIn>>();

            while (stillOk)
            {
                SyntaxParseResult<TIn> innerResult;
                if (innerClause is TerminalClause<TIn>)
                {
                    manyNode.IsManyTokens = true;
                    innerResult = ParseTerminal(tokens, innerClause as TerminalClause<TIn>, currentPosition);
                }
                else if (innerClause is NonTerminalClause<TIn> nonTerm)
                {
                    innerResult = ParseNonTerminal(tokens, nonTerm, currentPosition);
                    if (nonTerm.IsGroup)
                        manyNode.IsManyGroups = true;
                    else
                        manyNode.IsManyValues = true;
                }
                else if (innerClause is GroupClause<TIn>)
                {
                    manyNode.IsManyGroups = true;
                    innerResult = ParseNonTerminal(tokens, innerClause as NonTerminalClause<TIn>, currentPosition);
                }
                else
                {
                    throw new InvalidOperationException("unable to apply repeater to " + innerClause.GetType().Name);
                }

                if (innerResult != null && !innerResult.IsError)
                {
                    manyNode.Add(innerResult.Root);
                    currentPosition = innerResult.EndingPosition;
                    lastInnerResult = innerResult;
                }
                else
                {
                    if (innerResult != null)
                    {
                        innerErrors.AddRange(innerResult.Errors);
                    }
                }

                stillOk = stillOk && innerResult != null && !innerResult.IsError && currentPosition < tokens.Count;
            }


            result.EndingPosition = currentPosition;
            result.IsError = false;
            result.Errors = innerErrors;
            result.Root = manyNode;
            result.IsEnded = lastInnerResult != null && lastInnerResult.IsEnded;
            return result;
        }

        public SyntaxParseResult<TIn> ParseOneOrMore(IList<Token<TIn>> tokens, OneOrMoreClause<TIn> clause, int position)
        {
            var result = new SyntaxParseResult<TIn>();
            var manyNode = new ManySyntaxNode<TIn>("");
            var currentPosition = position;
            var innerClause = clause.Clause;
            bool isError;

            SyntaxParseResult<TIn> lastInnerResult = null;

            SyntaxParseResult<TIn> firstInnerResult;
            if (innerClause is TerminalClause<TIn>)
            {
                manyNode.IsManyTokens = true;
                firstInnerResult = ParseTerminal(tokens, innerClause as TerminalClause<TIn>, currentPosition);
            }
            else if (innerClause is NonTerminalClause<TIn>)
            {
                manyNode.IsManyValues = true;
                firstInnerResult = ParseNonTerminal(tokens, innerClause as NonTerminalClause<TIn>, currentPosition);
            }
            else
            {
                throw new InvalidOperationException("unable to apply repeater to " + innerClause.GetType().Name);
            }

            if (firstInnerResult != null && !firstInnerResult.IsError)
            {
                manyNode.Add(firstInnerResult.Root);
                lastInnerResult = firstInnerResult;
                currentPosition = firstInnerResult.EndingPosition;
                var more = new ZeroOrMoreClause<TIn>(innerClause);
                var nextResult = ParseZeroOrMore(tokens, more, currentPosition);
                if (nextResult != null && !nextResult.IsError)
                {
                    currentPosition = nextResult.EndingPosition;
                    var moreChildren = (ManySyntaxNode<TIn>) nextResult.Root;
                    manyNode.Children.AddRange(moreChildren.Children);
                }

                isError = false;
            }

            else
            {
                isError = true;
            }

            result.EndingPosition = currentPosition;
            result.IsError = isError;
            result.Root = manyNode;
            result.IsEnded = lastInnerResult != null && lastInnerResult.IsEnded;
            return result;
        }

        public SyntaxParseResult<TIn> ParseOption(IList<Token<TIn>> tokens, OptionClause<TIn> clause, Rule<TIn> rule,
            int position)
        {
            var result = new SyntaxParseResult<TIn>();
            var currentPosition = position;
            var innerClause = clause.Clause;

            SyntaxParseResult<TIn> innerResult;


            if (innerClause is TerminalClause<TIn>)
                innerResult = ParseTerminal(tokens, innerClause as TerminalClause<TIn>, currentPosition);
            else if (innerClause is NonTerminalClause<TIn>)
                innerResult = ParseNonTerminal(tokens, innerClause as NonTerminalClause<TIn>, currentPosition);
            else
                throw new InvalidOperationException("unable to apply repeater to " + innerClause.GetType().Name);


            if (innerResult.IsError)
            {
                if (innerClause is TerminalClause<TIn>)
                {
                    result = new SyntaxParseResult<TIn>();
                    result.IsError = true;
                    result.Root = new SyntaxLeaf<TIn>(Token<TIn>.Empty(), false);
                    result.EndingPosition = position;
                }
                else
                {
                    result = new SyntaxParseResult<TIn>();
                    result.IsError = true;
                    var children = new List<ISyntaxNode<TIn>> {innerResult.Root};
                    if (innerResult.IsError) children.Clear();
                    result.Root = new OptionSyntaxNode<TIn>(rule.NonTerminalName, children,
                        rule.GetVisitor());
                    (result.Root as OptionSyntaxNode<TIn>).IsGroupOption = clause.IsGroupOption;
                    result.EndingPosition = position;
                }
            }
            else
            {
                var node = innerResult.Root;

                var children = new List<ISyntaxNode<TIn>> {innerResult.Root};
                result.Root =
                    new OptionSyntaxNode<TIn>(rule.NonTerminalName, children, rule.GetVisitor());
                result.EndingPosition = innerResult.EndingPosition;
            }

            return result;
        }

        #endregion
    }
}