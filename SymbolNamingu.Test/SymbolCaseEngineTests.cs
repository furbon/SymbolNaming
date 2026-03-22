using SymbolNaming.Analysis;
using SymbolNaming.Conversion;
using SymbolNaming.Engine;
using SymbolNaming.Tokens;

namespace SymbolNaming.Test;

public class SymbolCaseEngineTests
{
    [Fact]
    public void Freeze前にAnalyzeするとInvalidOperationExceptionを送出する()
    {
        var engine = CreateEngine(freeze: false);

        Assert.Throws<InvalidOperationException>(() => engine.Analyze("UserName"));
    }

    [Fact]
    public void AnalyzeはTokenizerとClassifierを組み合わせてCase分類できる()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("s"));

        var result = engine.Analyze(
            "s_UserName",
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("s"),
            });

        Assert.Equal(CaseStyle.PascalCase, result.Style);
        Assert.True(result.Prefixed);
    }

    [Fact]
    public void TryAnalyzeは判定不能ケースでfalseとUnknownを返す()
    {
        var engine = CreateEngine();

        var success = engine.TryAnalyze("m_UserName", out var result);

        Assert.False(success);
        Assert.Equal(CaseStyle.Unknown, result.Style);
        Assert.False(result.Prefixed);
    }

    [Fact]
    public void Tokenizeは統合済みTokenizerの結果を返す()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("m"));

        var tokens = engine.Tokenize("mTestValue");

        Assert.Equal(new[] { TokenCategory.Prefix, TokenCategory.Word, TokenCategory.Word }, tokens.Select(t => t.Category).ToArray());
    }

    [Fact]
    public void ConvertはCase変換結果を返す()
    {
        var engine = CreateEngine();

        var converted = engine.Convert("UserName", CaseStyle.CamelCase);

        Assert.Equal("userName", converted);
    }

    [Fact]
    public void InspectはPrefixとCase情報とPrefix除去後シンボル名を取得できる()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("m"));

        var inspection = engine.Inspect(
            "m_UserName",
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("m"),
            });

        Assert.Equal(CaseStyle.PascalCase, inspection.CaseStyle);
        Assert.True(inspection.Prefixed);
        Assert.True(inspection.HasPrefix);
        Assert.Equal("m", inspection.Prefix);
        Assert.Equal("UserName", inspection.SymbolNameWithoutPrefix);
    }

    [Fact]
    public void InspectSpanはSpanベースでPrefixとPrefix除去後シンボル名を取得できる()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("s_"));

        var inspection = engine.Inspect(
            "s_UserName".AsSpan(),
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("s_"),
            });

        Assert.Equal(CaseStyle.PascalCase, inspection.CaseStyle);
        Assert.True(inspection.Prefixed);
        Assert.True(inspection.HasPrefix);
        Assert.Equal("s_", inspection.Prefix.ToString());
        Assert.Equal("UserName", inspection.SymbolNameWithoutPrefix.ToString());
    }

    private static SymbolCaseEngine CreateEngine(TestProtectedWordProvider? protectedWordProvider = null, TestPrefixProvider? prefixProvider = null, bool freeze = true)
    {
        var engine = new SymbolCaseEngine(
            TestTokenizerFactory.CreateDefault(protectedWordProvider, prefixProvider),
            new DefaultCaseClassifier(),
            new DefaultCaseConverter());

        if (freeze)
        {
            engine.Freeze();
        }

        return engine;
    }
}
