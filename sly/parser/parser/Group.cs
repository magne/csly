using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using sly.lexer;

namespace sly.parser.parser
{
    public class Group<TIn, TOut>
    {
        public List<GroupItem<TIn, TOut>> Items;

        public Dictionary<string, GroupItem<TIn, TOut>> ItemsByName;

        public Group()
        {
            Items = new List<GroupItem<TIn, TOut>>();
            ItemsByName = new Dictionary<string, GroupItem<TIn, TOut>>();
        }

        public int Count => Items.Count;


        public TOut Value(int i)
        {
            return Items[i].Value;
        }

        public Token<TIn> Token(int i)
        {
            return Items[i].Token;
        }


        public TOut Value(string name)
        {
            return ItemsByName.ContainsKey(name) ? ItemsByName[name].Value : default;
        }

        public Token<TIn> Token(string name)
        {
            return ItemsByName.ContainsKey(name) ? ItemsByName[name].Token : null;
        }

        public void Add(string name, Token<TIn> token)
        {
            var groupItem = new GroupItem<TIn, TOut>(name, token);
            Items.Add(groupItem);
            ItemsByName[name] = groupItem;
        }

        public void Add(string name, TOut value)
        {
            var groupItem = new GroupItem<TIn, TOut>(name, value);
            Items.Add(groupItem);
            ItemsByName[name] = groupItem;
        }


        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append("GROUP(");
            foreach (var item in Items)
            {
                builder.Append(item);
                builder.Append(",");
            }

            builder.Append(")");
            return builder.ToString();
        }
    }
}