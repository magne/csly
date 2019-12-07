using System;
using System.Collections.Generic;

namespace sly.lexer.fsm
{
    // ReSharper disable once InconsistentNaming
    public class FSMMatch<TNode>
    {
        public Dictionary<string, object> Properties { get; }

        public bool IsSuccess { get; }

        // ReSharper disable once InconsistentNaming
        public bool IsEOS { get; }

        public Token<TNode> Result { get; }

        public int NodeId { get; }

        public FSMMatch(bool success)
        {
            IsSuccess = success;
            IsEOS = !success;
        }

        public FSMMatch(bool success, TNode result, string value, TokenPosition position, int nodeId)
            : this(success, result, new ReadOnlyMemory<char>(value.ToCharArray()), position, nodeId)
        { }

        public FSMMatch(bool success, TNode result, ReadOnlyMemory<char> value, TokenPosition position, int nodeId)
        {
            Properties = new Dictionary<string, object>();
            IsSuccess = success;
            NodeId = nodeId;
            IsEOS = false;
            Result = new Token<TNode>(result, value, position);
        }
    }
}