namespace sly.v3.lexer.regex.dfalex
{
    internal interface IAppendable
    {
        IAppendable Append(string csq);

        IAppendable Append(char c);

        IAppendable Append(string csq, int start, int end);
    }
}