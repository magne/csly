namespace sly.lexer.fsm
{
    public class FSMNode<N>
    {
        internal FSMNode(int id)
        {
            Id = id;
        }

        internal int Id { get; }

        internal N Value { get; private set; }

        internal bool IsEnd { get; private set; }

        internal bool IsStart { get; set; }

        internal string Mark { private get; set; }

        internal void End(N value)
        {
            Value = value;
            IsEnd = true;
        }

        public override string ToString()
        {
            return $"\"{Mark ?? string.Empty} #{Id}\"";
        }
    }
}