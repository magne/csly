using System;
using BenchmarkDotNet.Running;

namespace bench
{
    internal static class Program
    {
        private static void BenchJson()
        {
            BenchmarkRunner.Run<JsonParserBench>();
        }

        // ReSharper disable once UnusedMember.Local
        private static void BenchGenericLexer()
        {
            BenchmarkRunner.Run<GenericLexerBench>();
        }

        private static void Main()
        {
            try
            {
                Console.WriteLine("Hello World!");
                BenchJson();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}