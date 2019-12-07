using System.Diagnostics.CodeAnalysis;

namespace csly.whileLang.compiler
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WhileType
    {
        BOOL,
        INT,
        STRING,
        ANY,
        NONE
    }
}