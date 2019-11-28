using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace bench.json.model
{
    public class JObject : Json
    {
        public JObject(string key, Json value)
        {
            Values = new Dictionary<string, Json>();
            Values[key] = value;
        }

        public JObject()
        {
            Values = new Dictionary<string, Json>();
        }

        public JObject(Dictionary<string, Json> dic)
        {
            Values = dic;
        }

        public override bool IsObject => true;

        public override bool IsList => true;

        private Dictionary<string, Json> Values { get; }

        public int Count => Values.Count;

        public Json this[string key]
        {
            get => Values[key];
            set => Values[key] = value;
        }


        public void Merge(JObject obj)
        {
            foreach (var pair in obj.Values) this[pair.Key] = pair.Value;
        }

        public bool ContainsKey(string key)
        {
            return Values.ContainsKey(key);
        }
    }
}