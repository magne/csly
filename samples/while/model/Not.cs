﻿using System;
using System.Text;
using csly.whileLang.compiler;
using sly.lexer;
using Sigil;

namespace csly.whileLang.model
{
    public class Not : IExpression
    {
        public Not(IExpression value)
        {
            Value = value;
        }

        public IExpression Value { get; set; }

        public Scope CompilerScope { get; set; }

        public TokenPosition Position { get; set; }

        public WhileType Whiletype
        {
            get => WhileType.BOOL;
            set { }
        }


        public string Dump(string tab)
        {
            var dmp = new StringBuilder();
            dmp.AppendLine($"{tab}(NOT");
            dmp.AppendLine(Value.Dump(tab + "\t"));
            dmp.AppendLine($"{tab})");
            return dmp.ToString();
        }

        public string Transpile(CompilerContext context)
        {
            return $"! {Value.Transpile(context)}";
        }

        public Emit<Func<int>> EmitByteCode(CompilerContext context, Emit<Func<int>> emiter)
        {
            emiter = Value.EmitByteCode(context, emiter);
            emiter.Negate();
            return emiter;
        }
    }
}