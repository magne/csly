﻿using System;
using System.Text;
using csly.whileLang.compiler;
using sly.lexer;
using Sigil;

namespace csly.whileLang.model
{
    public class WhileStatement : IStatement
    {
        public WhileStatement(IExpression condition, IStatement blockStmt)
        {
            Condition = condition;
            BlockStmt = blockStmt;
        }

        public IExpression Condition { get; set; }

        public IStatement BlockStmt { get; set; }

        public Scope CompilerScope { get; set; }

        public TokenPosition Position { get; set; }

        public string Dump(string tab)
        {
            var dmp = new StringBuilder();
            dmp.AppendLine($"{tab}(WHILE");

            dmp.AppendLine($"{tab + "\t"}(COND");
            dmp.AppendLine(Condition.Dump("\t\t" + tab));
            dmp.AppendLine($"{tab + "\t"})");

            dmp.AppendLine($"{tab + "\t"}(BLOCK");
            dmp.AppendLine(BlockStmt.Dump("\t\t" + tab));
            dmp.AppendLine($"{tab})");

            dmp.AppendLine($"{tab})");
            return dmp.ToString();
        }

        public string Transpile(CompilerContext context)
        {
            var code = new StringBuilder();
            code.AppendLine($"while({Condition.Transpile(context)}) {{ ");
            code.AppendLine(BlockStmt.Transpile(context));
            code.AppendLine("}");
            return code.ToString();
        }

        public Emit<Func<int>> EmitByteCode(CompilerContext context, Emit<Func<int>> emiter)
        {
            var loopLabel = emiter.DefineLabel();
            var outLabel = emiter.DefineLabel();

            emiter.MarkLabel(loopLabel);
            Condition.EmitByteCode(context, emiter);
            emiter.BranchIfFalse(outLabel);
            BlockStmt.EmitByteCode(context, emiter);
            emiter.Branch(loopLabel);
            emiter.MarkLabel(outLabel);
            return emiter;
        }
    }
}