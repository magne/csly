using System.Collections.Generic;
using System.Linq;
using sly.lexer;
using sly.parser.generator;
using sly.parser.syntax.grammar;
using sly.parser.syntax.tree;

namespace sly.parser.llparser
{
    public class RecursiveDescentSyntaxParser<TIn, TOut> : ISyntaxParser<TIn, TOut> where TIn : struct
    {
        public RecursiveDescentSyntaxParser(ParserConfiguration<TIn, TOut> configuration, string startingNonTerminal)
        {
            Configuration = configuration;
            StartingNonTerminal = startingNonTerminal;
            ComputeSubRules(configuration);
            InitializeStartingTokens(Configuration, startingNonTerminal);
        }

        public ParserConfiguration<TIn, TOut> Configuration { get; set; }
        public string StartingNonTerminal { get; set; }

        public ParserConfiguration<TIn, TOut> ComputeSubRules(ParserConfiguration<TIn, TOut> configuration)
        {
            var newNonTerms = new List<NonTerminal<TIn>>();
            foreach (var nonTerm in configuration.NonTerminals)
            foreach (var rule in nonTerm.Value.Rules)
            {
                var newclauses = new List<IClause<TIn>>();
                if (rule.ContainsSubRule)
                {
                    foreach (var clause in rule.Clauses)
                        if (clause is GroupClause<TIn> group)
                        {
                            var newNonTerm = CreateSubRule(group);
                            newNonTerms.Add(newNonTerm);
                            var newClause = new NonTerminalClause<TIn>(newNonTerm.Name);
                            newClause.IsGroup = true;
                            newclauses.Add(newClause);
                        }
                        else if (clause is ManyClause<TIn> many)
                        {
                            if (many.Clause is GroupClause<TIn> manyGroup)
                            {
                                var newNonTerm = CreateSubRule(manyGroup);
                                newNonTerms.Add(newNonTerm);
                                var newInnerNonTermClause = new NonTerminalClause<TIn>(newNonTerm.Name);
                                newInnerNonTermClause.IsGroup = true;
                                many.Clause = newInnerNonTermClause;
                                newclauses.Add(many);
                            }
                        }
                        else if (clause is OptionClause<TIn> option)
                        {
                            if (option.Clause is GroupClause<TIn> optionGroup)
                            {
                                var newNonTerm = CreateSubRule(optionGroup);
                                newNonTerms.Add(newNonTerm);
                                var newInnerNonTermClause = new NonTerminalClause<TIn>(newNonTerm.Name);
                                newInnerNonTermClause.IsGroup = true;
                                option.Clause = newInnerNonTermClause;
                                newclauses.Add(option);
                            }
                        }
                        else
                        {
                            newclauses.Add(clause);
                        }

                    rule.Clauses.Clear();
                    rule.Clauses.AddRange(newclauses);
                }
            }

            newNonTerms.ForEach(nonTerminal => configuration.AddNonTerminalIfNotExists(nonTerminal));
            return configuration;
        }

        public NonTerminal<TIn> CreateSubRule(GroupClause<TIn> group)
        {
            var subRuleNonTerminalName = "GROUP-" + group.Clauses.Select(c => c.ToString())
                                             .Aggregate((c1, c2) => $"{c1.ToString()}-{c2.ToString()}");
            var nonTerminal = new NonTerminal<TIn>(subRuleNonTerminalName);
            var subRule = new Rule<TIn>();
            subRule.Clauses = group.Clauses;
            subRule.IsSubRule = true;
            nonTerminal.Rules.Add(subRule);
            nonTerminal.IsSubRule = true;

            return nonTerminal;
        }

        #region STARTING_TOKENS

        protected virtual void InitializeStartingTokens(ParserConfiguration<TIn, TOut> configuration, string root)
        {
            var nts = configuration.NonTerminals;


            InitStartingTokensForNonTerminal(nts, root);
            foreach (var nt in nts.Values)
            {
                foreach (var rule in nt.Rules)
                {
                    if (rule.PossibleLeadingTokens == null || rule.PossibleLeadingTokens.Count == 0)
                        InitStartingTokensForRule(nts, rule);
                }
            }
        }

        protected virtual void InitStartingTokensForNonTerminal(Dictionary<string, NonTerminal<TIn>> nonTerminals,
            string name)
        {
            if (nonTerminals.ContainsKey(name))
            {
                var nt = nonTerminals[name];
                nt.Rules.ForEach(r => InitStartingTokensForRule(nonTerminals, r));
            }
        }

        protected virtual void InitStartingTokensForRule(Dictionary<string, NonTerminal<TIn>> nonTerminals,
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
                        rule.PossibleLeadingTokens.Add(term.ExpectedToken);
                        rule.PossibleLeadingTokens = rule.PossibleLeadingTokens.Distinct().ToList();
                    }
                    else
                    {
                        var nonterm = first as NonTerminalClause<TIn>;
                        InitStartingTokensForNonTerminal(nonTerminals, nonterm.NonTerminalName);
                        if (nonTerminals.ContainsKey(nonterm.NonTerminalName))
                        {
                            var firstNonTerminal = nonTerminals[nonterm.NonTerminalName];
                            firstNonTerminal.Rules.ForEach(r => { rule.PossibleLeadingTokens.AddRange(r.PossibleLeadingTokens); });
                            rule.PossibleLeadingTokens = rule.PossibleLeadingTokens.Distinct().ToList();
                        }
                    }
                }
            }
        }

        #endregion

        #region parsing

        public SyntaxParseResult<TIn> Parse(IList<Token<TIn>> tokens, string startingNonTerminal = null)
        {
            var start = startingNonTerminal ?? StartingNonTerminal;
            var nonTerminals = Configuration.NonTerminals;
            var errors = new List<UnexpectedTokenSyntaxError<TIn>>();
            var nt = nonTerminals[start];

            var rules = nt.Rules.Where(r => r.PossibleLeadingTokens.Contains(tokens[0].TokenID)).ToList();

            if (!rules.Any())
            {
                errors.Add(new UnexpectedTokenSyntaxError<TIn>(tokens[0], nt.PossibleLeadingTokens.ToArray()));
            }

            var rs = new List<SyntaxParseResult<TIn>>();
            foreach (var rule in rules)
            {
                var r = Parse(tokens, rule, 0, start);
                rs.Add(r);
            }

            SyntaxParseResult<TIn> result = null;


            if (rs.Count > 0)
            {
                result = rs.Find(r => r.IsEnded && !r.IsError);

                if (result == null)
                {
                    var endingPositions = rs.Select(r => r.EndingPosition).ToList();
                    var lastposition = endingPositions.Max();
                    var furtherResults = rs.Where(r => r.EndingPosition == lastposition).ToList();

                    errors.Add(new UnexpectedTokenSyntaxError<TIn>(tokens[lastposition], null));
                    furtherResults.ForEach(r =>
                    {
                        if (r.Errors != null) errors.AddRange(r.Errors);
                    });
                }
            }

            if (result == null)
            {
                result = new SyntaxParseResult<TIn>();
                errors.Sort();

                if (errors.Count > 0)
                {
                    var lastErrorPosition = errors.Select(e => e.UnexpectedToken.PositionInTokenFlow).ToList().Max();
                    var lastErrors = errors.Where(e => e.UnexpectedToken.PositionInTokenFlow == lastErrorPosition)
                        .ToList();
                    result.Errors = lastErrors;
                }
                else
                {
                    result.Errors = errors;
                }

                result.IsError = true;
            }

            return result;
        }


        public virtual SyntaxParseResult<TIn> Parse(IList<Token<TIn>> tokens, Rule<TIn> rule, int position,
            string nonTerminalName)
        {
            var currentPosition = position;
            var errors = new List<UnexpectedTokenSyntaxError<TIn>>();
            var isError = false;
            var children = new List<ISyntaxNode<TIn>>();
            if (rule.PossibleLeadingTokens.Contains(tokens[position].TokenID))
                if (rule.Clauses != null && rule.Clauses.Count > 0)
                {
                    children = new List<ISyntaxNode<TIn>>();
                    foreach (var clause in rule.Clauses)
                    {
                        if (clause is TerminalClause<TIn>)
                        {
                            var termRes = ParseTerminal(tokens, clause as TerminalClause<TIn>, currentPosition);
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
                                children.Add(nonTerminalResult.Root);
                                currentPosition = nonTerminalResult.EndingPosition;
                                if (nonTerminalResult.Errors != null && nonTerminalResult.Errors.Any())
                                    errors.AddRange(nonTerminalResult.Errors);
                            }
                            else
                            {
                                errors.AddRange(nonTerminalResult.Errors);
                            }

                            isError = nonTerminalResult.IsError;
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
                    node = new GroupSyntaxNode<TIn>(nonTerminalName, children);
                else
                    node = new SyntaxNode<TIn>(nonTerminalName, children);
                node = ManageExpressionRules(rule, node);
                if (node.IsByPassNode) // inutile de créer un niveau supplémentaire
                    result.Root = children[0];
                result.Root = node;
                result.IsEnded = result.EndingPosition >= tokens.Count - 1
                                 || result.EndingPosition == tokens.Count - 2 &&
                                 tokens[tokens.Count - 1].TokenID.Equals(default(TIn));
            }


            return result;
        }

        protected SyntaxNode<TIn> ManageExpressionRules(Rule<TIn> rule, SyntaxNode<TIn> node)
        {
            var operatorIndex = -1;
            if (rule.IsExpressionRule && rule.IsByPassRule)
            {
                node.IsByPassNode = true;
            }
            else if (rule.IsExpressionRule && !rule.IsByPassRule)
            {
                node.ExpressionAffix = rule.ExpressionAffix;
                if (node.Children.Count == 3)
                {
                    operatorIndex = 1;
                }
                else if (node.Children.Count == 2)
                {
                    if (node.ExpressionAffix == Affix.PreFix)
                        operatorIndex = 0;
                    else if (node.ExpressionAffix == Affix.PostFix) operatorIndex = 1;
                }

                if (operatorIndex >= 0)
                    if (node.Children[operatorIndex] is SyntaxLeaf<TIn> operatorNode)
                    {
                        var visitor = rule.GetVisitor(operatorNode.Token.TokenID);
                        if (visitor != null)
                        {
                            node.Visitor = visitor;
                            node.Operation = rule.GetOperation(operatorNode.Token.TokenID);
                        }
                    }
            }
            else if (!rule.IsExpressionRule)
            {
                node.Visitor = rule.GetVisitor();
            }

            return node;
        }

        public SyntaxParseResult<TIn> ParseTerminal(IList<Token<TIn>> tokens, TerminalClause<TIn> terminal, int position)
        {
            var result = new SyntaxParseResult<TIn>();
            result.IsError = !terminal.Check(tokens[position].TokenID);
            result.EndingPosition = !result.IsError ? position + 1 : position;
            var token = tokens[position];
            token.Discarded = terminal.Discarded;
            result.Root = new SyntaxLeaf<TIn>(token, terminal.Discarded);
            return result;
        }


        public SyntaxParseResult<TIn> ParseNonTerminal(IList<Token<TIn>> tokens, NonTerminalClause<TIn> nonTermClause,
            int currentPosition)
        {
            var startPosition = currentPosition;
            var nt = Configuration.NonTerminals[nonTermClause.NonTerminalName];
            var errors = new List<UnexpectedTokenSyntaxError<TIn>>();

            var i = 0;

            var allAcceptableTokens = new List<TIn>();
            nt.Rules.ForEach(r =>
            {
                if (r != null && r.PossibleLeadingTokens != null) allAcceptableTokens.AddRange(r.PossibleLeadingTokens);
            });
            allAcceptableTokens = allAcceptableTokens.Distinct().ToList();

            var rules = nt.Rules
                .Where(r => r.PossibleLeadingTokens.Contains(tokens[startPosition].TokenID) || r.MayBeEmpty)
                .ToList();

            if (rules.Count == 0)
                errors.Add(new UnexpectedTokenSyntaxError<TIn>(tokens[startPosition],
                    allAcceptableTokens.ToArray<TIn>()));

            var innerRuleErrors = new List<UnexpectedTokenSyntaxError<TIn>>();
            SyntaxParseResult<TIn> okResult = null;
            var greaterIndex = 0;
            var allRulesInError = true;
            while (i < rules.Count)
            {
                var innerrule = rules[i];
                var innerRuleRes = Parse(tokens, innerrule, startPosition, nonTermClause.NonTerminalName);
                if (!innerRuleRes.IsError && okResult == null ||
                    okResult != null && innerRuleRes.EndingPosition > okResult.EndingPosition)
                {
                    okResult = innerRuleRes;
                    okResult.Errors = innerRuleRes.Errors;
                }

                var other = greaterIndex == 0 && innerRuleRes.EndingPosition == 0;
                if (innerRuleRes.EndingPosition > greaterIndex && innerRuleRes.Errors != null &&
                    !innerRuleRes.Errors.Any() || other)
                {
                    greaterIndex = innerRuleRes.EndingPosition;
                    innerRuleErrors.Clear();
                    innerRuleErrors.AddRange(innerRuleRes.Errors);
                }

                innerRuleErrors.AddRange(innerRuleRes.Errors);
                allRulesInError = allRulesInError && innerRuleRes.IsError;
                i++;
            }

            errors.AddRange(innerRuleErrors);

            var result = new SyntaxParseResult<TIn>();
            result.Errors = errors;
            if (okResult != null)
            {
                result.Root = okResult.Root;
                result.IsError = false;
                result.EndingPosition = okResult.EndingPosition;
                result.IsEnded = okResult.IsEnded;

                result.Errors = errors;
            }
            else
            {
                result.IsError = true;
                result.Errors = errors;
                greaterIndex = errors.Select(e => e.UnexpectedToken.PositionInTokenFlow).Max();
                result.EndingPosition = greaterIndex;
            }

            return result;
        }

        public virtual void Init(ParserConfiguration<TIn, TOut> configuration, string root)
        {
            if (root != null) StartingNonTerminal = root;
            InitializeStartingTokens(configuration, StartingNonTerminal);
        }

        #endregion
    }
}