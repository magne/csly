using System;
using BenchmarkDotNet.Running;

namespace bench
{
    internal static class Program
    {
        private static void BenchJson()
        {
            JsonParserBench.Versions = new[]
            {
                "2.2.5.1",
                "2.2.5.2",
                "2.2.5.3",
                "2.3.0.1"
            };
            BenchmarkRunner.Run<JsonParserBench>();
        }

        // ReSharper disable once UnusedMember.Local
        private static void BenchGenericLexer()
        {
            GenericLexerBench.Versions = new[]
            {
                "2.2.5.1",
                "2.2.5.2",
                "2.2.5.3",
                "2.3.0.1"
            };
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