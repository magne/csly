using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using sly.parser.generator;

namespace sly.parser.syntax.grammar
{
    public class Rule<TIn> : GrammarNode<TIn> where TIn : struct
    {
        public Rule()
        {
            Clauses = new List<IClause<TIn>>();
            VisitorMethodsForOperation = new Dictionary<TIn, OperationMetaData<TIn>>();
            Visitor = null;
            IsSubRule = false;
        }

        public bool IsByPassRule { get; set; } = false;

        // visitors for operation rules
        private Dictionary<TIn, OperationMetaData<TIn>> VisitorMethodsForOperation { get; }

        // visitor for classical rules
        private MethodInfo Visitor { get; set; }

        public bool IsExpressionRule { get; set; }

        public Affix ExpressionAffix { get; set; }

        public string RuleString { get; }

        public string Key
        {
            get
            {
                var k = Clauses
                    .Select(c => c.ToString())
                    .Aggregate((c1, c2) => c1.ToString() + "_" + c2.ToString());
                if (Clauses.Count == 1) k += "_";
                return k;
            }
        }

        public List<IClause<TIn>> Clauses { get; set; }
        public List<TIn> PossibleLeadingTokens { get; set; }

        public string NonTerminalName { get; set; }

        public bool ContainsSubRule
        {
            get
            {
                if (Clauses != null && Clauses.Any())
                    foreach (var clause in Clauses)
                    {
                        if (clause is GroupClause<TIn>) return true;
                        if (clause is ManyClause<TIn> many) return many.Clause is GroupClause<TIn>;
                        if (clause is OptionClause<TIn> option) return option.Clause is GroupClause<TIn>;
                    }

                return false;
            }
        }

        public bool IsSubRule { get; set; }

        public bool MayBeEmpty => Clauses == null
                                  || Clauses.Count == 0
                                  || Clauses.Count == 1 && Clauses[0].MayBeEmpty();


        public OperationMetaData<TIn> GetOperation(TIn token = default(TIn))
        {
            if (IsExpressionRule)
            {
                var operation = VisitorMethodsForOperation.ContainsKey(token)
                    ? VisitorMethodsForOperation[token]
                    : null;
                return operation;
            }

            return null;
        }

        public MethodInfo GetVisitor(TIn token = default(TIn))
        {
            MethodInfo visitor;
            if (IsExpressionRule)
            {
                var operation = VisitorMethodsForOperation.ContainsKey(token)
                    ? VisitorMethodsForOperation[token]
                    : null;
                visitor = operation?.VisitorMethod;
            }
            else
            {
                visitor = Visitor;
            }

            return visitor;
        }

        public void SetVisitor(MethodInfo visitor)
        {
            Visitor = visitor;
        }

        public void SetVisitor(OperationMetaData<TIn> operation)
        {
            VisitorMethodsForOperation[operation.OperatorToken] = operation;
        }
    }
}