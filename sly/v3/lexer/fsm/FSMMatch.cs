using System;
using System.Collections.Generic;

namespace sly.v3.lexer.fsm
{
    // ReSharper disable once InconsistentNaming
    internal class FSMMatch<T>
    {
        public Dictionary<string, object> Properties { get; }

        public bool IsSuccess { get; }

        // ReSharper disable once InconsistentNaming
        public bool IsEOS { get; }

        public Token<T> Result { get; }

        public int NodeId { get; }

        public FSMMatch(bool success)
        {
            IsSuccess = success;
            IsEOS = !success;
        }

        public FSMMatch(bool success, T result, string value, TokenPosition position, int nodeId)
            : this(success, result, new ReadOnlyMemory<char>(value.ToCharArray()), position, nodeId)
        { }

        public FSMMatch(bool success, T result, ReadOnlyMemory<char> value, TokenPosition position, int nodeId)
        {
            Properties = new Dictionary<string, object>();
            IsSuccess = success;
            NodeId = nodeId;
            IsEOS = false;
            Result = new Token<T>(result, value, position);
        }
    }
}