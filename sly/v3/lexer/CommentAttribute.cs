﻿using System;

namespace sly.v3.lexer
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal class CommentAttribute : Attribute
    {
        public string MultiLineCommentEnd;

        public string MultiLineCommentStart;

        public string SingleLineCommentStart;

        public CommentAttribute(string singleLineStart, string multiLineStart, string multiLineEnd)
        {
            SingleLineCommentStart = singleLineStart;
            MultiLineCommentStart = multiLineStart;
            MultiLineCommentEnd = multiLineEnd;
        }
    }
}