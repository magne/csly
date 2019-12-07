using System.Collections.Generic;
using bench.json.model;
using sly.lexer;
using sly.parser.generator;

// ReSharper disable once CheckNamespace
namespace bench.json
{
    public static class DictionaryExtensionMethods
    {
        public static void Merge<TKey, TValue>(this Dictionary<TKey, TValue> me, Dictionary<TKey, TValue> merge)
        {
            foreach (var item in merge) me[item.Key] = item.Value;
        }
    }

    public class JsonParser
    {
        #region root

        [Production("root : value")]
        public Json Root(Json value)
        {
            return value;
        }

        #endregion

        #region VALUE

        [Production("value : STRING")]
        public Json StringValue(Token<JsonToken> stringToken)
        {
            return new JValue(stringToken.StringWithoutQuotes);
        }

        [Production("value : INT")]
        public Json IntValue(Token<JsonToken> intToken)
        {
            return new JValue(intToken.IntValue);
        }

        [Production("value : DOUBLE")]
        public Json DoubleValue(Token<JsonToken> doubleToken)
        {
            double dbl;
            try
            {
                var doubleParts = doubleToken.Value.Split('.');
                dbl = double.Parse(doubleParts[0]);
                if (doubleParts.Length > 1)
                {
                    var decimalPart = double.Parse(doubleParts[1]);
                    for (var i = 0; i < doubleParts[1].Length; i++) decimalPart = decimalPart / 10.0;
                    dbl += decimalPart;
                }
            }
            catch
            {
                dbl = double.MinValue;
            }

            return new JValue(dbl);
        }

        [Production("value : BOOLEAN")]
        public Json BooleanValue(Token<JsonToken> boolToken)
        {
            return new JValue(bool.Parse(boolToken.Value));
        }

        [Production("value : NULL")]
        public Json NullValue(object forget)
        {
            return new JNull();
        }

        [Production("value : object")]
        public Json ObjectValue(Json value)
        {
            return value;
        }

        [Production("value: list")]
        public Json ListValue(JList list)
        {
            return list;
        }

        #endregion

        #region OBJECT

        [Production("object: ACCG ACCD")]
        public Json EmptyObjectValue(object accg, object accd)
        {
            return new JObject();
        }

        [Production("object: ACCG members ACCD")]
        public Json AttributesObjectValue(object accg, JObject members, object accd)
        {
            return members;
        }

        #endregion

        #region LIST

        [Production("list: CROG CROD")]
        public Json EmptyList(object crog, object crod)
        {
            return new JList();
        }

        [Production("list: CROG listElements CROD")]
        public Json List(object crog, JList elements, object crod)
        {
            return elements;
        }


        [Production("listElements: value COMMA listElements")]
        public Json ListElementsMany(Json value, object comma, JList tail)
        {
            var elements = new JList(value);
            elements.AddRange(tail);
            return elements;
        }

        [Production("listElements: value")]
        public Json ListElementsOne(Json element)
        {
            return new JList(element);
        }

        #endregion

        #region PROPERTIES

        [Production("property: STRING COLON value")]
        public Json property(Token<JsonToken> key, object colon, Json value)
        {
            return new JObject(key.StringWithoutQuotes, value);
        }


        [Production("members : property COMMA members")]
        public Json ManyMembers(JObject pair, object comma, JObject tail)
        {
            var members = new JObject();
            members.Merge(pair);
            members.Merge(tail);
            return members;
        }

        [Production("members : property")]
        public Json SingleMember(JObject pair)
        {
            var members = new JObject();
            members.Merge(pair);
            return members;
        }

        #endregion
    }
}