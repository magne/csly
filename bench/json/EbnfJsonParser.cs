using System;
using System.Collections.Generic;
using System.Linq;
using bench.json.model;
using sly.lexer;
using sly.parser.generator;
using sly.parser.parser;

// ReSharper disable once CheckNamespace
namespace bench.json
{
    public class EbnfJsonParser
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
        public object IntValue(Token<JsonToken> intToken)
        {
            return new JValue(intToken.IntValue);
        }

        [Production("value : DOUBLE")]
        public object DoubleValue(Token<JsonToken> doubleToken)
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
            catch (Exception)
            {
                dbl = double.MinValue;
            }

            return new JValue(dbl);
        }

        [Production("value : BOOLEAN")]
        public object BooleanValue(Token<JsonToken> boolToken)
        {
            return new JValue(bool.Parse(boolToken.Value));
        }

        [Production("value : NULL")]
        public object NullValue(object forget)
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


        [Production("listElements: value (COMMA [d] value)*")]
        public Json ListElements(Json head, List<Group<JsonToken, Json>> tail)
        {
            var values = new JList(head);
            values.AddRange(tail.Select(group => group.Value(0)).ToList());
            return values;
        }

        [Production("additionalValue: COMMA value")]
        public Json ListElementsOne(Token<JsonToken> discardedComma, Json value)
        {
            return value;
        }

        #endregion

        #region PROPERTIES

        [Production("members: property additionalProperty*")]
        public object Members(JObject head, List<Json> tail)
        {
            var value = new JObject();
            value.Merge(head);
            foreach (var p in tail) value.Merge((JObject) p);
            return value;
        }

        [Production("additionalProperty : COMMA property")]
        public object Property(Token<JsonToken> comma, JObject property)
        {
            return property;
        }

        [Production("property: STRING COLON value")]
        public object Property(Token<JsonToken> key, object colon, Json value)
        {
            return new JObject(key.StringWithoutQuotes, value);
        }


        //[Production("members : property COMMA members")]
        //public  object ManyMembers(KeyValuePair<string, object> pair, object comma, Dictionary<string, object> tail)
        //{
        //    Dictionary<string, object> members = new Dictionary<string, object>();
        //    members[pair.Key] = pair.Value;
        //    members.Merge(tail);
        //    return members;
        //}

        //[Production("members : property")]
        //public  object SingleMember(KeyValuePair<string, object> pair)
        //{
        //    Dictionary<string, object> members = new Dictionary<string, object>();
        //    members.Add(pair.Key, pair.Value);
        //    return members;
        //}

        #endregion
    }
}