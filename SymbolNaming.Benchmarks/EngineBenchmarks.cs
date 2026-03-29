using BenchmarkDotNet.Attributes;
using SymbolNaming;
using SymbolNaming.Analysis;
using SymbolNaming.Conversion;
using SymbolNaming.Dictionaries;
using SymbolNaming.Engine;
using SymbolNaming.Tokenization;

namespace SymbolNaming.Benchmarks;

[MemoryDiagnoser]
public class EngineBenchmarks
{
    private static readonly CaseAnalysisOptions AnalysisOptions = new()
    {
        PrefixProvider = new PrefixSetProvider("m", "s", "k"),
    };

    private SymbolCaseEngine _engine = null!;

    [Params(
        "UserName",
        "m_UserName",
        "EnemyUserData_WALK_NORMAL",
        "HTTPServer",
        "__built_in_process__")]
    public string Symbol { get; set; } = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _engine = new SymbolCaseEngine(
            SymbolTokenizerFactory.CreateDefault(),
            new DefaultCaseClassifier(),
            new DefaultCaseConverter());
    }

    [Benchmark]
    public CaseClassificationResult Analyze()
    {
        return _engine.Analyze(Symbol, AnalysisOptions);
    }

    [Benchmark]
    public SymbolInspection Inspect()
    {
        return _engine.Inspect(Symbol, AnalysisOptions);
    }

    [Benchmark]
    public CaseConversionResult ConvertToScreamingSnake()
    {
        return _engine.Convert(Symbol, CaseStyle.ScreamingSnakeCase);
    }
}
