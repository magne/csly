using System;
using System.IO;
using bench.json;
using bench.json.model;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using sly.parser;
using sly.parser.generator;

namespace bench
{
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class JsonParserBench
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                var baseJob = Job.MediumRun.With(CsProjCoreToolchain.NetCoreApp20);
                foreach (var version in Versions)
                {
                    Add(baseJob.WithNuGet("sly", version).WithId(version));
                }

                Add(EnvironmentAnalyser.Default);
            }
        }

        public static string[] Versions { get; set; } = new string[0];

        private Parser<JsonTokenGeneric, Json> benchedParser;

        private string content = "";

        [GlobalSetup]
        public void Setup()
        {
            Console.WriteLine(("SETUP"));
            content = File.ReadAllText("test.json");
            Console.WriteLine("json read.");
            var jsonParser = new EbnfJsonGenericParser();
            var builder = new ParserBuilder<JsonTokenGeneric, Json>();

            var result = builder.BuildParser(jsonParser, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
            Console.WriteLine("parser built.");
            if (result.IsError)
            {
                Console.WriteLine("ERROR");
                result.Errors.ForEach(Console.WriteLine);
            }
            else
            {
                Console.WriteLine("parser ok");
                benchedParser = result.Result;
            }

            Console.WriteLine($"parser {benchedParser}");
        }

        [Benchmark]
        public void TestJson()
        {
            if (benchedParser == null)
            {
                Console.WriteLine("parser is null");
            }
            else
            {
                var _ = benchedParser.Parse(content);
            }
        }
    }
}