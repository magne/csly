using sly.buildresult;
using sly.lexer;
using sly.lexer.fsm;

namespace sly.v3.lexer
{
    internal static class LexerBuilder
    {
        internal static BuildResult<ILexer<TLexeme>> BuildLexer<TLexeme>(BuildResult<ILexer<TLexeme>> result, BuildExtension<TLexeme> extensionBuilder)
            where TLexeme : struct
        {
            var attributes = sly.lexer.LexerBuilder.GetLexemes(result);
            result = sly.lexer.LexerBuilder.Build(attributes, result, extensionBuilder);
            return result;
        }
    }
}