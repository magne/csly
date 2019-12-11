using System;

namespace sly.v3.lexer.regex.dfalex
{
    /// <summary>
    /// An <see cref="IAppendable"/> for string replacements that will allocate a new string buffer only when the first difference is written.
    /// </summary>
    internal class StringReplaceAppendable : IAppendable
    {
        private readonly string src;
        private          char[] buf;
        private          int    len;

        public StringReplaceAppendable(string src)
        {
            this.src = src;
        }

        public IAppendable Append(string csq)
        {
            Append(csq, 0, csq.Length);
            return this;
        }

        public IAppendable Append(char c)
        {
            if (buf != null)
            {
                if (len >= buf.Length)
                {
                    var tempBuf = new char[buf.Length * 2];
                    Array.Copy(buf, tempBuf, buf.Length);
                    buf = tempBuf;
                }

                buf[len++] = c;
                return this;
            }

            if (len < src.Length && src[len] == c)
            {
                ++len;
                return this;
            }

            Allocate(1);
            buf[len++] = c;
            return this;
        }

        public IAppendable Append(string csq, int start, int end)
        {
            if (start < 0 || end < start)
            {
                throw new IndexOutOfRangeException();
            }

            if (buf == null)
            {
                if (csq == src && start == len)
                {
                    if (end > src.Length)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    len = end;
                    return this;
                }

                for (;; ++start, ++len)
                {
                    if (start >= end)
                    {
                        return this;
                    }

                    if (len >= src.Length || src[len] != csq[start])
                    {
                        break;
                    }
                }

                //new data - need to allocate
                Allocate(end - start);
            }
            else if (buf.Length - len < end - start)
            {
                var tempBuf = new char[Math.Max(buf.Length * 2, len + (end -start))];
                Array.Copy(buf, tempBuf, buf.Length);
                buf = tempBuf;
            }

            if (csq is string str)
            {
                str.CopyTo(start, buf, len, end - start);
                len += end - start;
            }
            else
            {
                while (start < end)
                {
                    buf[len++] = csq[start++];
                }
            }
            return this;
        }

        public override string ToString()
        {
            if (buf != null)
            {
                return new string(buf, 0, len);
            }

            if (len == src.Length)
            {
                return src;
            }

            return src.Substring(0, len);
        }

        private void Allocate(int addLen)
        {
            buf = new char[Math.Max(len + addLen, src.Length + 16)];
            src.CopyTo(0, buf, 0, len);
        }
    }
}