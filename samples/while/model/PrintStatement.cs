﻿using System;
using System.Text;
using csly.whileLang.compiler;
using sly.lexer;
using Sigil;

namespace csly.whileLang.model
{
    public class PrintStatement : IStatement
    {
        public PrintStatement(IExpression value)
        {
            Value = value;
        }

        public IExpression Value { get; set; }

        public Scope CompilerScope { get; set; }

        public TokenPosition Position { get; set; }

        public string Dump(string tab)
        {
            var dmp = new StringBuilder();
            dmp.AppendLine($"{tab}(PRINT ");
            dmp.AppendLine($"{Value.Dump("\t" + tab)}");
            dmp.AppendLine($"{tab})");
            return dmp.ToString();
        }

        public string Transpile(CompilerContext context)
        {
            return $"System.Console.WriteLine({Value.Transpile(context)});";
        }

        public Emit<Func<int>> EmitByteCode(CompilerContext context, Emit<Func<int>> emiter)
        {
            var mi = typeof(Console).GetMethod("WriteLine", new[] {typeof(string)});

            emiter = Value.EmitByteCode(context, emiter);
            emiter.Call(mi);

            return emiter;
        }
    }
}