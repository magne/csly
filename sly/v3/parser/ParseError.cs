namespace sly.v3.parser
{
    internal class ParseError
    {
        public virtual int Column { get; protected set; }
        public virtual string ErrorMessage { get; protected set; }
        public virtual int Line { get; protected set; }


        //public ParseError(int line, int column)
        //{
        //    Column = column;
        //    Line = line;
        //}

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
    }
}