using System.Collections.Generic;

namespace sly.v3.lexer
{
    internal static class DictionaryExtensions
    {
        // ReSharper disable once UseDeconstructionOnParameter
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
}