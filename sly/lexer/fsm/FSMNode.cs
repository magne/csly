namespace sly.lexer.fsm
{
    // ReSharper disable once InconsistentNaming
    public class FSMNode<TNode>
    {
        internal FSMNode(TNode value)
        {
            Value = value;
        }

        internal TNode Value { get; set; }

        internal int Id { get; set; }

        internal bool IsEnd { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        internal bool IsStart { get; set; }
        public string Mark { get; internal set; }
    }
}