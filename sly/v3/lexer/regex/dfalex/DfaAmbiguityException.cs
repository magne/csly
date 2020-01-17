using System;
using System.Collections.Generic;
using System.Text;

namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// Exception thrown by default when patterns for multiple results match the same string in a DFA, and no way has
    /// been provided to combine result.
    /// </summary>
    internal class DfaAmbiguityException : Exception
    {
        /// <summary>
        /// Create a new AmbiguityException.
        /// </summary>
        /// <param name="results">the multiple results for patters that match the same string</param>
        public DfaAmbiguityException(IEnumerable<object> results)
            : this(new Initializer(null, results))
        { }

        /// <summary>
        /// Create a new AmbiguityException.
        /// </summary>
        /// <param name="message">The exception detail message</param>
        /// <param name="results">the multiple results for patters that match the same string</param>
        public DfaAmbiguityException(string message, IEnumerable<object> results)
            : this(new Initializer(message, results))
        { }

        private DfaAmbiguityException(Initializer inivals)
            : base(inivals.Message)
        {
            Results = inivals.Results;
        }

        /// <summary>
        /// Get the set of results that can match the same string.
        /// </summary>
        /// <returns>set of conflicting results</returns>
        public List<object> Results { get; }

        private class Initializer
        {
            internal readonly string       Message;
            internal readonly List<object> Results;

            internal Initializer(string message, IEnumerable<object> results)
            {
                Results = new List<object>(results);

                if (message == null)
                {
                    var sb = new StringBuilder();
                    sb.Append("The same string can match multiple patterns for: ");
                    var sep = "";
                    foreach (var result in Results)
                    {
                        sb.Append(sep).Append(result);
                        sep = ", ";
                    }

                    message = sb.ToString();
                }

                Message = message;
            }
        }
    }
}