using SymbolNaming.Analysis;
using SymbolNaming.Conversion;
using SymbolNaming.Engine;
using SymbolNaming.Tokens;

namespace SymbolNaming.Test;

public class SymbolCaseEngineTests
{
    [Fact]
    public void Engine生成時に自動Freezeされる()
    {
        var engine = CreateEngine();

        Assert.True(engine.IsFrozen);

        var result = engine.Analyze("UserName");

        Assert.Equal(CaseStyle.PascalCase, result.Style);
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
    public void AnalyzeSpanはSpan入力でCase分類できる()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("s_"));

        var result = engine.Analyze(
            "s_UserName".AsSpan(),
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("s_"),
            });

        Assert.Equal(CaseStyle.PascalCase, result.Style);
        Assert.True(result.Prefixed);
    }

    [Fact]
    public void TryAnalyzeSpanは判定不能ケースでfalseとUnknownを返す()
    {
        var engine = CreateEngine();

        var success = engine.TryAnalyze("m_UserName".AsSpan(), out var result);

        Assert.False(success);
        Assert.Equal(CaseStyle.Unknown, result.Style);
        Assert.False(result.Prefixed);
    }

    [Theory]
    [InlineData("_userName", CaseStyle.CamelCase)]
    [InlineData("__built_in_process", CaseStyle.LowerSnakeCase)]
    [InlineData("__USER_NAME__", CaseStyle.ScreamingSnakeCase)]
    public void Analyzeは先頭末尾アンダースコア装飾を除いてCase分類できる(string input, CaseStyle expected)
    {
        var engine = CreateEngine();

        var result = engine.Analyze(input);

        Assert.Equal(expected, result.Style);
        Assert.False(result.Prefixed);
    }

    [Fact]
    public void Analyzeは先頭末尾アンダースコア装飾情報を返す()
    {
        var engine = CreateEngine();

        var result = engine.Analyze("__userName__");

        Assert.Equal(CaseStyle.CamelCase, result.Style);
        Assert.Equal(2, result.Decoration.LeadingUnderscoreCount);
        Assert.Equal(2, result.Decoration.TrailingUnderscoreCount);
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

        Assert.Equal("userName", converted.Output);
        Assert.Equal(PrefixPolicy.Keep, converted.AppliedPrefixPolicy);
    }

    [Fact]
    public void ConvertSpanはCase変換結果を返す()
    {
        var engine = CreateEngine();

        var converted = engine.Convert("UserName".AsSpan(), CaseStyle.CamelCase);

        Assert.Equal("userName", converted.Output);
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
    public void Inspectは装飾情報を取得できる()
    {
        var engine = CreateEngine();

        var inspection = engine.Inspect("__built_in_process__");

        Assert.Equal(CaseStyle.LowerSnakeCase, inspection.CaseStyle);
        Assert.Equal(2, inspection.LeadingUnderscoreCount);
        Assert.Equal(2, inspection.TrailingUnderscoreCount);
    }

    [Fact]
    public void InspectはカスタムInspectionRuleをパイプライン順で実行できる()
    {
        var engine = CreateEngine(
            inspectionRules: new IInspectionRule[]
            {
                new FixedWarningRule(1, 1),
                new FixedWarningRule(3, 2),
            });

        var inspection = engine.Inspect("UserName");

        Assert.True(inspection.HasWarnings);
        Assert.Equal(2, inspection.Warnings.Count);
        Assert.Equal(1, inspection.Warnings[0].Start);
        Assert.Equal(3, inspection.Warnings[1].Start);
    }

    [Fact]
    public void InspectはPLayerパターンに警告を付与できる()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("s_"));

        var inspection = engine.Inspect(
            "s_PLayer",
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("s_"),
            });

        Assert.Equal(CaseStyle.PascalCase, inspection.CaseStyle);
        Assert.True(inspection.Prefixed);
        Assert.True(inspection.HasWarnings);
        Assert.Contains(inspection.Warnings, w => w.Kind == SymbolInspectionWarningKind.SuspiciousLeadingSingleUpperToken);
    }

    [Fact]
    public void InspectはInterface命名パターンIAsyncResultには警告しない()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("I"));

        var inspection = engine.Inspect(
            "IAsyncResult",
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("I"),
            });

        Assert.Equal(CaseStyle.PascalCase, inspection.CaseStyle);
        Assert.True(inspection.Prefixed);
        Assert.False(inspection.HasWarnings);
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

    [Fact]
    public void InspectSpanは装飾情報を取得できる()
    {
        var engine = CreateEngine();

        var inspection = engine.Inspect("__USER_NAME__".AsSpan());

        Assert.Equal(CaseStyle.ScreamingSnakeCase, inspection.CaseStyle);
        Assert.Equal(2, inspection.LeadingUnderscoreCount);
        Assert.Equal(2, inspection.TrailingUnderscoreCount);
    }

    [Theory]
    [InlineData("EnemyUserData_WALK", "UpperTagOrSegments", "EnemyUserData", "WALK")]
    [InlineData("EnemyUserData_WALK_NORMAL", "UpperTagOrSegments", "EnemyUserData", "WALK_NORMAL")]
    [InlineData("EnemyUserData_WALK_SP", "UpperTagOrSegments", "EnemyUserData", "WALK_SP")]
    [InlineData("EnemyUserData_Attack_R2", "PascalOrAlphaNumSegments", "EnemyUserData", "Attack_R2")]
    [InlineData("EnemyUserData_GuardAll", "PascalOrAlphaNumSegments", "EnemyUserData", "GuardAll")]
    public void Inspectは可変サフィックスの複合パターン一致を取得できる(string input, string expectedPatternId, string expectedBaseName, string expectedSuffix)
    {
        var engine = CreateEngine();

        var inspection = engine.Inspect(
            input,
            new CaseAnalysisOptions
            {
                CompositePatternMatcher = CreateGameCompositeMatcher(),
            });

        Assert.True(inspection.HasCompositePattern);
        Assert.NotNull(inspection.CompositePattern);
        Assert.Equal(expectedPatternId, inspection.CompositePattern.Value.PatternId);
        Assert.Equal(expectedBaseName, inspection.CompositePatternBaseName);
        Assert.Equal(expectedSuffix, inspection.CompositePatternSuffix);
    }

    [Fact]
    public void InspectはPrefix付きでも複合パターン一致を取得できる()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("s_"));

        var inspection = engine.Inspect(
            "s_EnemyUserData_WALK_NORMAL",
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("s_"),
                CompositePatternMatcher = CreateGameCompositeMatcher(),
            });

        Assert.True(inspection.HasCompositePattern);
        Assert.Equal("EnemyUserData", inspection.CompositePatternBaseName);
        Assert.Equal("WALK_NORMAL", inspection.CompositePatternSuffix);
    }

    [Fact]
    public void InspectSpanは複合パターン一致のSpan情報を取得できる()
    {
        var engine = CreateEngine();

        var inspection = engine.Inspect(
            "EnemyUserData_Attack_R2".AsSpan(),
            new CaseAnalysisOptions
            {
                CompositePatternMatcher = CreateGameCompositeMatcher(),
            });

        Assert.True(inspection.HasCompositePattern);
        Assert.Equal("EnemyUserData", inspection.CompositePatternBaseName.ToString());
        Assert.Equal("Attack_R2", inspection.CompositePatternSuffix.ToString());
    }

    [Fact]
    public void Inspectはルール不一致なら複合パターン一致なしを返す()
    {
        var engine = CreateEngine();

        var inspection = engine.Inspect(
            "EnemyUserData-WALK",
            new CaseAnalysisOptions
            {
                CompositePatternMatcher = CreateGameCompositeMatcher(),
            });

        Assert.False(inspection.HasCompositePattern);
        Assert.Null(inspection.CompositePattern);
        Assert.Null(inspection.CompositePatternBaseName);
        Assert.Null(inspection.CompositePatternSuffix);
    }

    private static SymbolCaseEngine CreateEngine(
        TestProtectedWordProvider? protectedWordProvider = null,
        TestPrefixProvider? prefixProvider = null,
        IReadOnlyList<IInspectionRule>? inspectionRules = null)
    {
        return new SymbolCaseEngine(
            TestTokenizerFactory.CreateDefault(protectedWordProvider, prefixProvider),
            new DefaultCaseClassifier(),
            new DefaultCaseConverter(),
            inspectionRules);
    }

    private static ICompositeSymbolPatternMatcher CreateGameCompositeMatcher()
    {
        return new CompositeSuffixPatternMatcher(
            new RegexCompositeSuffixPatternRule(
                "UpperTagOrSegments",
                "^[A-Z]+(?:_[A-Z0-9]+)*$",
                "^[A-Z][A-Za-z0-9]*$"),
            new RegexCompositeSuffixPatternRule(
                "PascalOrAlphaNumSegments",
                "^[A-Z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)*$",
                "^[A-Z][A-Za-z0-9]*$"));
    }

    private sealed class FixedWarningRule : IInspectionRule
    {
        private readonly SymbolInspectionWarning _warning;

        public FixedWarningRule(int start, int length)
        {
            _warning = new SymbolInspectionWarning(SymbolInspectionWarningKind.SuspiciousLeadingSingleUpperToken, start, length);
        }

        public bool TryCreateWarning(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification, out SymbolInspectionWarning warning)
        {
            warning = _warning;
            return true;
        }
    }
}
