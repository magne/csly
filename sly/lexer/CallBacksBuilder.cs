using System;
using System.Linq;
using System.Reflection;

namespace sly.lexer
{
    public class CallBacksBuilder
    {

        public static void BuildCallbacks<TLexeme>(GenericLexer<TLexeme> lexer) where TLexeme : struct
        {
            var attributes =
                (CallBacksAttribute[]) typeof(TLexeme).GetCustomAttributes(typeof(CallBacksAttribute), true);
            Type callbackClass = attributes[0].CallBacksClass;
            ExtractCallBacks(callbackClass,lexer);

        }

        public static void ExtractCallBacks<TLexeme>(Type callbackClass, GenericLexer<TLexeme> lexer) where TLexeme : struct
        {
            var methods = callbackClass.GetMethods().ToList();
            methods = methods.Where(m =>
            {
                var attributes = m.GetCustomAttributes().ToList();
                var attr = attributes.Find(a => a.GetType() == typeof(TokenCallbackAttribute));
                return m.IsStatic && attr != null;
            }).ToList();

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(TokenCallbackAttribute), false).Cast<TokenCallbackAttribute>().ToList();
                AddCallback(lexer, method, EnumConverter.ConvertIntToEnum<TLexeme>(attributes[0].EnumValue));
            }
        }

        public static void AddCallback<TLexeme>(GenericLexer<TLexeme> lexer, MethodInfo method, TLexeme token) where TLexeme : struct
        {
            var callbackDelegate = (Func<Token<TLexeme>,Token<TLexeme>>)Delegate.CreateDelegate(typeof(Func<Token<TLexeme>,Token<TLexeme>>), method);
            lexer.AddCallBack(token,callbackDelegate);
        }
    }
}