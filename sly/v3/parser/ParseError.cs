namespace sly.v3.parser
{
    internal abstract class ParseError
    {
        protected ParseError(int line, int column)
        {
            Column = column;
            Line = line;
        }

        public int Line { get; }

        public int Column { get; }

        public abstract string ErrorMessage { get; }

        public int CompareTo(object obj)
        {
            var comparison = 0;
            if (obj is ParseError unexpectedError)
            {
                var lineComparison = Line.CompareTo(unexpectedError.Line);

                if (lineComparison > 0) comparison = 1;
                if (lineComparison == 0) comparison = Column.CompareTo(unexpectedError.Column);
                if (lineComparison < 0) comparison = -1;
            }

            return comparison;
        }

        public override string ToString()
        {
            return ErrorMessage;
        }
    }
}