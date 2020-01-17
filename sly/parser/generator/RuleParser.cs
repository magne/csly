using System;
using sly.lexer;
using sly.parser.syntax.grammar;

namespace sly.parser.generator
{
    public class RuleParser<TIn> where TIn : struct
    {
        #region rules grammar

        [Production("rule : IDENTIFIER COLON clauses")]
        public object Root(Token<EbnfTokenGeneric> name, Token<EbnfTokenGeneric> discarded, ClauseSequence<TIn> clauses)
        {
            var rule = new Rule<TIn>();
            rule.NonTerminalName = name.Value;
            rule.Clauses = clauses.Clauses;
            return rule;
        }


        [Production("clauses : clause clauses")]
        public object Clauses(IClause<TIn> clause, ClauseSequence<TIn> clauses)
        {
            var list = new ClauseSequence<TIn>(clause);
            if (clauses != null) list.AddRange(clauses);
            return list;
        }

        [Production("clauses : clause ")]
        public ClauseSequence<TIn> SingleClause(IClause<TIn> clause)
        {
            return new ClauseSequence<TIn>(clause);
        }


        [Production("clause : IDENTIFIER ZEROORMORE")]
        public IClause<TIn> ZeroMoreClause(Token<EbnfTokenGeneric> id, Token<EbnfTokenGeneric> discarded)
        {
            var innerClause = BuildTerminalOrNonTerimal(id.Value, true);
            return new ZeroOrMoreClause<TIn>(innerClause);
        }

        [Production("clause : IDENTIFIER ONEORMORE")]
        public IClause<TIn> OneMoreClause(Token<EbnfTokenGeneric> id, Token<EbnfTokenGeneric> discarded)
        {
            var innerClause = BuildTerminalOrNonTerimal(id.Value);
            return new OneOrMoreClause<TIn>(innerClause);
        }

        [Production("clause : IDENTIFIER OPTION")]
        public IClause<TIn> OptionClause(Token<EbnfTokenGeneric> id, Token<EbnfTokenGeneric> discarded)
        {
            var innerClause = BuildTerminalOrNonTerimal(id.Value);
            return new OptionClause<TIn>(innerClause);
        }

        [Production("clause : IDENTIFIER DISCARD ")]
        public IClause<TIn> SimpleDiscardedClause(Token<EbnfTokenGeneric> id, Token<EbnfTokenGeneric> discard)
        {
            var clause = BuildTerminalOrNonTerimal(id.Value, true);
            return clause;
        }

        [Production("clause : choiceclause DISCARD")]
        public IClause<TIn> AlternateDiscardedClause(ChoiceClause<TIn> choices, Token<EbnfTokenGeneric> discarded)
        {
            choices.IsDiscarded = true;
            return choices;
        }

        [Production("clause : choiceclause")]
        public IClause<TIn> AlternateClause(ChoiceClause<TIn> choices)
        {
            choices.IsDiscarded = false;
            return choices;
        }

        [Production("choiceclause : LCROG  choices RCROG  ")]
        public IClause<TIn> AlternateChoices(Token<EbnfTokenGeneric> discardleft, IClause<TIn> choices, Token<EbnfTokenGeneric> discardright)
        {
            // TODO
            return choices;
        }

        [Production("choices : IDENTIFIER  ")]
        public IClause<TIn> ChoicesOne(Token<EbnfTokenGeneric> head)
        {
            // TODO
            var choice = BuildTerminalOrNonTerimal(head.Value);
            return new ChoiceClause<TIn>(choice);
        }

        [Production("choices : IDENTIFIER OR choices ")]
        public IClause<TIn> ChoicesMany(Token<EbnfTokenGeneric> head, Token<EbnfTokenGeneric> discardOr, ChoiceClause<TIn> tail)
        {
            var headClause = BuildTerminalOrNonTerimal(head.Value);
            return new ChoiceClause<TIn>(headClause, tail.Choices);
        }


        [Production("clause : IDENTIFIER ")]
        public IClause<TIn> SimpleClause(Token<EbnfTokenGeneric> id)
        {
            var clause = BuildTerminalOrNonTerimal(id.Value);
            return clause;
        }


        #region groups

        [Production("clause : LPAREN  groupclauses RPAREN ")]
        public GroupClause<TIn> Group(Token<EbnfTokenGeneric> discardLeft, GroupClause<TIn> clauses,
            Token<EbnfTokenGeneric> discardRight)
        {
            return clauses;
        }

        [Production("clause : choiceclause ONEORMORE ")]
        public IClause<TIn> ChoiceOneOrMore(ChoiceClause<TIn> choices, Token<EbnfTokenGeneric> discardOneOrMore)
        {
            return new OneOrMoreClause<TIn>(choices);
        }

        [Production("clause : choiceclause ZEROORMORE ")]
        public IClause<TIn> ChoiceZeroOrMore(ChoiceClause<TIn> choices, Token<EbnfTokenGeneric> discardZeroOrMore)
        {
            return new ZeroOrMoreClause<TIn>(choices);
        }


        [Production("clause : choiceclause OPTION ")]
        public IClause<TIn> ChoiceOptional(ChoiceClause<TIn> choices, Token<EbnfTokenGeneric> discardOption)
        {
            return new OptionClause<TIn>(choices);
        }

        [Production("clause : LPAREN  groupclauses RPAREN ONEORMORE ")]
        public IClause<TIn> GroupOneOrMore(Token<EbnfTokenGeneric> discardLeft, GroupClause<TIn> clauses,
            Token<EbnfTokenGeneric> discardRight, Token<EbnfTokenGeneric> oneZeroOrMore)
        {
            return new OneOrMoreClause<TIn>(clauses);
        }

        [Production("clause : LPAREN  groupclauses RPAREN ZEROORMORE ")]
        public IClause<TIn> GroupZeroOrMore(Token<EbnfTokenGeneric> discardLeft, GroupClause<TIn> clauses,
            Token<EbnfTokenGeneric> discardRight, Token<EbnfTokenGeneric> discardZeroOrMore)
        {
            return new ZeroOrMoreClause<TIn>(clauses);
        }

        [Production("clause : LPAREN  groupclauses RPAREN OPTION ")]
        public IClause<TIn> GroupOptional(Token<EbnfTokenGeneric> discardLeft, GroupClause<TIn> group,
            Token<EbnfTokenGeneric> discardRight, Token<EbnfTokenGeneric> option)
        {
            return new OptionClause<TIn>(group);
        }


        [Production("groupclauses : groupclause groupclauses")]
        public object GroupClauses(GroupClause<TIn> group, GroupClause<TIn> groups)
        {
            if (groups != null) group.AddRange(groups);
            return group;
        }

        [Production("groupclauses : groupclause")]
        public object GroupClausesOne(GroupClause<TIn> group)
        {
            return group;
        }

        [Production("groupclause : IDENTIFIER ")]
        public GroupClause<TIn> GroupClause(Token<EbnfTokenGeneric> id)
        {
            var clause = BuildTerminalOrNonTerimal(id.Value);
            return new GroupClause<TIn>(clause);
        }

        [Production("groupclause : IDENTIFIER DISCARD ")]
        public GroupClause<TIn> GroupClauseDiscarded(Token<EbnfTokenGeneric> id, Token<EbnfTokenGeneric> discarded)
        {
            var clause = BuildTerminalOrNonTerimal(id.Value, true);
            return new GroupClause<TIn>(clause);
        }

        #endregion


        private IClause<TIn> BuildTerminalOrNonTerimal(string name, bool discard = false)
        {
            IClause<TIn> clause;
            var isTerminal = false;
            var b = Enum.TryParse(name, out TIn token);
            if (b)
            {
                isTerminal = true;
            }

            if (isTerminal)
                clause = new TerminalClause<TIn>(token, discard);
            else
                clause = new NonTerminalClause<TIn>(name);
            return clause;
        }

        #endregion
    }
}