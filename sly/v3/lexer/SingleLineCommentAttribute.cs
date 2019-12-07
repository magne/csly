using System;

namespace sly.v3.lexer
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal class SingleLineCommentAttribute : CommentAttribute
    {
        public SingleLineCommentAttribute(string start) : base(start, null, null)
        { }
    }
}