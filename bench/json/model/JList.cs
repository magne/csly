using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace bench.json.model
{
    public class JList : Json

    {
        public JList()
        {
            Items = new List<Json>();
        }

        public JList(List<Json> lst)
        {
            Items = lst;
        }


        public JList(Json item)
        {
            Items = new List<Json>();
            Items.Add(item);
        }

        public override bool IsList => true;

        public List<Json> Items { get; }

        public int Count => Items.Count;

        public Json this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public void Add(Json item)
        {
            Items.Add(item);
        }

        public void AddRange(JList items)
        {
            Items.AddRange(items.Items);
        }

        public void AddRange(List<Json> items)
        {
            Items.AddRange(items);
        }
    }
}