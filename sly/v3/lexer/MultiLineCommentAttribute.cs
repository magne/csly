using System;

namespace sly.v3.lexer
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal class MultiLineCommentAttribute : CommentAttribute
    {
        public MultiLineCommentAttribute(string start, string end) : base(null, start, end)
        { }
    }
}