using System.IO;
using bench.json;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using sly.buildresult;
using sly.lexer;

namespace bench
{
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class GenericLexerBench
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

        private ILexer<JsonTokenGeneric> benchedLexer;

        private string content = "";

        [GlobalSetup]
        public void Setup()
        {
            content = File.ReadAllText("test.json");

            var lexerRes = LexerBuilder.BuildLexer(new BuildResult<ILexer<JsonTokenGeneric>>());
            if (lexerRes != null)
            {
                benchedLexer = lexerRes.Result;
            }
        }

        [Benchmark]
        public void TestJson()
        {
            var _ = benchedLexer.Tokenize(content);
        }
    }
}