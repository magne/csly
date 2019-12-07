﻿using System;
using csly.whileLang.compiler;
using sly.lexer;
using Sigil;

namespace csly.whileLang.model
{
    public interface IWhileAst
    {
        TokenPosition Position { get; set; }

        Scope CompilerScope { get; set; }
        string Dump(string tab);

        string Transpile(CompilerContext context);
        Emit<Func<int>> EmitByteCode(CompilerContext context, Emit<Func<int>> emiter);
    }
}