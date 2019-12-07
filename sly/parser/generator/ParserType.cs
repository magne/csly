using System.Diagnostics.CodeAnalysis;

namespace sly.parser.generator
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ParserType
    {
        LL_RECURSIVE_DESCENT = 1,
        EBNF_LL_RECURSIVE_DESCENT = 2
    }
}