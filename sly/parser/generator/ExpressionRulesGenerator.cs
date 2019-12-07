using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using sly.buildresult;
using sly.parser.syntax.grammar;

namespace sly.parser.generator
{
    public class OperationMetaData<T> where T : struct
    {
        public OperationMetaData(int precedence, Associativity assoc, MethodInfo method, Affix affix, T oper)
        {
            Precedence = precedence;
            Associativity = assoc;
            VisitorMethod = method;
            OperatorToken = oper;
            Affix = affix;
        }

        public int Precedence { get; set; }

        public Associativity Associativity { get; set; }

        public MethodInfo VisitorMethod { get; set; }

        public T OperatorToken { get; set; }

        public Affix Affix { get; set; }

        public bool IsBinary => Affix == Affix.InFix;

        public bool IsUnary => Affix != Affix.InFix;

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return $"{OperatorToken} / {Affix} : {Precedence} / {Associativity}";
        }
    }

    public class ExpressionRulesGenerator
    {
        public static BuildResult<ParserConfiguration<TIn, TOut>> BuildExpressionRules<TIn, TOut>(
            ParserConfiguration<TIn, TOut> configuration, Type parserClass,
            BuildResult<ParserConfiguration<TIn, TOut>> result) where TIn : struct
        {
            var methods = parserClass.GetMethods().ToList();
            methods = methods.Where(m =>
            {
                var attributes = m.GetCustomAttributes().ToList();
                var attr = attributes.Find(a => a.GetType() == typeof(OperationAttribute));
                return attr != null;
            }).ToList();


            var operationsByPrecedence = new Dictionary<int, List<OperationMetaData<TIn>>>();


            methods.ForEach(m =>
            {
                var attributes =
                    (OperationAttribute[]) m.GetCustomAttributes(typeof(OperationAttribute), true);

                foreach (var attr in attributes)
                {
                    var operation = new OperationMetaData<TIn>(attr.Precedence, attr.Assoc, m, attr.Affix,
                        EnumConverter.ConvertIntToEnum<TIn>(attr.Token));
                    var operations = new List<OperationMetaData<TIn>>();
                    if (operationsByPrecedence.ContainsKey(operation.Precedence))
                        operations = operationsByPrecedence[operation.Precedence];
                    operations.Add(operation);
                    operationsByPrecedence[operation.Precedence] = operations;
                }
            });

            if (operationsByPrecedence.Count > 0)
            {
                methods = parserClass.GetMethods().ToList();
                var operandMethod = methods.Find(m =>
                {
                    var attributes = m.GetCustomAttributes().ToList();
                    var attr = attributes.Find(a => a.GetType() == typeof(OperandAttribute));
                    return attr != null;
                });

                string operandNonTerminal = null;

                if (operandMethod == null)
                {
                    result.AddError(new ParserInitializationError(ErrorLevel.FATAL, "missing [operand] attribute"));
                    throw new Exception("missing [operand] attribute");
                }

                var production =
                    operandMethod.GetCustomAttributes().ToList()
                        .Find(attr => attr.GetType() == typeof(ProductionAttribute)) as ProductionAttribute;
                if (production != null)
                {
                    var ruleItems = production.RuleString.Split(':');
                    if (ruleItems.Length > 0) operandNonTerminal = ruleItems[0].Trim();
                }


                if (operandNonTerminal != null && operationsByPrecedence.Count > 0)
                    GenerateExpressionParser(configuration, operandNonTerminal, operationsByPrecedence,
                        parserClass.Name);
            }

            result.Result = configuration;
            return result;
        }


        private static void GenerateExpressionParser<TIn, TOut>(ParserConfiguration<TIn, TOut> configuration,
            string operandNonTerminal, Dictionary<int, List<OperationMetaData<TIn>>> operationsByPrecedence,
            string parserClassName) where TIn : struct
        {
            var precedences = operationsByPrecedence.Keys.ToList();
            precedences.Sort();

            for (var i = 0; i < precedences.Count; i++)
            {
                var precedence = precedences[i];
                var nextPrecedence = i < precedences.Count - 1 ? precedences[i + 1] : -1;
                var operations = operationsByPrecedence[precedence];
                var name = GetNonTerminalNameForPrecedence(precedence, operationsByPrecedence, operandNonTerminal);
                var nextName =
                    GetNonTerminalNameForPrecedence(nextPrecedence, operationsByPrecedence, operandNonTerminal);

                var nonTerminal = BuildNonTerminal(i == precedences.Count - 1, name, nextName, operations,
                    operationsByPrecedence);

                configuration.NonTerminals[nonTerminal.Name] = nonTerminal;
            }

            // entry point non terminal
            var entrypoint = new NonTerminal<TIn>($"{parserClassName}_expressions", new List<Rule<TIn>>());
            var prec = precedences[0];
            var lowestname = GetNonTerminalNameForPrecedence(prec, operationsByPrecedence, operandNonTerminal);
            var rule = new Rule<TIn>();
            rule.Clauses.Add(new NonTerminalClause<TIn>(lowestname));
            rule.IsByPassRule = true;
            rule.IsExpressionRule = true;
            rule.ExpressionAffix = Affix.NotOperator;
            configuration.NonTerminals[entrypoint.Name] = entrypoint;
            entrypoint.Rules.Add(rule);
        }


        private static NonTerminal<TIn> BuildNonTerminal<TIn>(bool last, string name, string nextName,
            List<OperationMetaData<TIn>> operations, Dictionary<int, List<OperationMetaData<TIn>>> operationsByPrecedence)
            where TIn : struct
        {
            var nonTerminal = new NonTerminal<TIn>(name, new List<Rule<TIn>>());
            foreach (var operation in operations)
                if (operation.Affix == Affix.InFix)
                {
                    var rule = new Rule<TIn>();
                    rule.Clauses.Add(new NonTerminalClause<TIn>(nextName));
                    rule.Clauses.Add(new TerminalClause<TIn>(operation.OperatorToken));
                    rule.Clauses.Add(new NonTerminalClause<TIn>(name));
                    rule.IsExpressionRule = true;
                    rule.ExpressionAffix = operation.Affix;
                    rule.SetVisitor(operation);
                    nonTerminal.Rules.Add(rule);
                }
                else if (operation.Affix == Affix.PreFix)
                {
                    var rule = new Rule<TIn>();
                    rule.Clauses.Add(new TerminalClause<TIn>(operation.OperatorToken));
                    rule.Clauses.Add(new NonTerminalClause<TIn>(nextName));
                    rule.IsExpressionRule = true;
                    rule.ExpressionAffix = operation.Affix;
                    rule.SetVisitor(operation);
                    nonTerminal.Rules.Add(rule);
                }
                else if (operation.Affix == Affix.PostFix)
                {
                    var rule = new Rule<TIn>();
                    rule.Clauses.Add(new NonTerminalClause<TIn>(nextName));
                    rule.Clauses.Add(new TerminalClause<TIn>(operation.OperatorToken));
                    rule.IsExpressionRule = true;
                    rule.ExpressionAffix = operation.Affix;
                    rule.SetVisitor(operation);
                    nonTerminal.Rules.Add(rule);
                }

            var rule0 = new Rule<TIn>();
            rule0.Clauses.Add(new NonTerminalClause<TIn>(nextName));
            rule0.IsExpressionRule = true;
            rule0.ExpressionAffix = Affix.NotOperator;
            rule0.IsByPassRule = true;
            nonTerminal.Rules.Add(rule0);

            return nonTerminal;
        }

        private static string GetNonTerminalNameForPrecedence<TIn>(int precedence,
            Dictionary<int, List<OperationMetaData<TIn>>> operationsByPrecedence, string operandName) where TIn : struct
        {
            if (precedence > 0)
            {
                var tokens = operationsByPrecedence[precedence].Select(o => o.OperatorToken).ToList();
                return GetNonTerminalNameForPrecedence(precedence, tokens);
            }

            return operandName;
        }

        private static string GetNonTerminalNameForPrecedence<TIn>(int precedence, List<TIn> operators) where TIn : struct
        {
            var operatorsPart = operators
                .Select(oper => oper.ToString())
                .ToList()
                .Aggregate((s1, s2) => $"{s1}_{s2}");
            var name = $"expr_{precedence}_{operatorsPart}";


            return name;
        }
    }
}