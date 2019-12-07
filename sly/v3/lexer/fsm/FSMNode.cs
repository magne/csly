namespace sly.v3.lexer.fsm
{
    internal delegate FSMMatch<TIn> NodeCallback<TIn>(FSMMatch<TIn> node);

    // ReSharper disable once InconsistentNaming
    internal class FSMNode<T>
    {
        internal FSMNode(int id)
        {
            Id = id;
        }

        internal int Id { get; }

        internal T Value { get; private set; }

        internal bool IsEnd { get; private set; }

        internal bool IsStart { get; set; }

        internal bool HasCallback => Callback != null;

        internal NodeCallback<T> Callback { get; set; }

        internal string Mark { private get; set; }

        internal void End(T value)
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