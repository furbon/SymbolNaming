using SymbolNaming.Analysis;
using SymbolNaming.Conversion;
using SymbolNaming.Engine;
using SymbolNaming.Tokens;

namespace SymbolNaming.Tests;

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
        Assert.True(inspection.HasPrefix);
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
        Assert.True(inspection.HasPrefix);
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

    [Fact]
    public void NormalizeForAnalysisは正規化シンボルと装飾情報を返す()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("m"));

        var normalized = engine.NormalizeForAnalysis(
            "__m_user_name__",
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("m"),
            });

        Assert.Equal(CaseStyle.LowerSnakeCase, normalized.CaseStyle);
        Assert.True(normalized.HasPrefix);
        Assert.Equal("m", normalized.Prefix);
        Assert.Equal("user_name__", normalized.SymbolNameWithoutPrefix);
        Assert.Equal("user_name", normalized.NormalizedSymbol);
        Assert.Equal(0, normalized.LeadingUnderscoreCount);
        Assert.Equal(2, normalized.TrailingUnderscoreCount);
    }

    [Fact]
    public void NormalizeForAnalysisSpanは正規化シンボルSpanを返す()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("m"));

        var normalized = engine.NormalizeForAnalysis(
            "m_user_name__".AsSpan(),
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("m"),
            });

        Assert.Equal(CaseStyle.LowerSnakeCase, normalized.CaseStyle);
        Assert.True(normalized.HasPrefix);
        Assert.Equal("m", normalized.Prefix.ToString());
        Assert.Equal("user_name__", normalized.SymbolNameWithoutPrefix.ToString());
        Assert.Equal("user_name", normalized.NormalizedSymbol.ToString());
        Assert.Equal(0, normalized.LeadingUnderscoreCount);
        Assert.Equal(2, normalized.TrailingUnderscoreCount);
    }

    [Fact]
    public void NormalizeForAnalysisはAnalyzeと装飾情報が整合する()
    {
        var engine = CreateEngine();

        var analyzed = engine.Analyze("__built_in_process__");
        var normalized = engine.NormalizeForAnalysis("__built_in_process__");

        Assert.Equal(analyzed.Style, normalized.CaseStyle);
        Assert.Equal(analyzed.Decoration.LeadingUnderscoreCount, normalized.LeadingUnderscoreCount);
        Assert.Equal(analyzed.Decoration.TrailingUnderscoreCount, normalized.TrailingUnderscoreCount);
        Assert.Equal("built_in_process", normalized.NormalizedSymbol);
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

        Assert.True(inspection.CompositePattern.HasValue);
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

        Assert.True(inspection.CompositePattern.HasValue);
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

        Assert.True(inspection.CompositePattern.HasValue);
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

        Assert.False(inspection.CompositePattern.HasValue);
        Assert.Null(inspection.CompositePattern);
        Assert.Null(inspection.CompositePatternBaseName);
        Assert.Null(inspection.CompositePatternSuffix);
    }

    [Fact]
    public void AnalyzeManyは入力順序を維持して結果を返す()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("s_"));

        var results = engine.AnalyzeMany(
            new[] { "UserName", "s_UserName" },
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("s_"),
            });

        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.Equal(0, results[0].Index);
        Assert.Equal(CaseStyle.PascalCase, results[0].Value!.Style);
        Assert.False(results[0].Value.Prefixed);

        Assert.True(results[1].IsSuccess);
        Assert.Equal(1, results[1].Index);
        Assert.Equal(CaseStyle.PascalCase, results[1].Value!.Style);
        Assert.True(results[1].Value.Prefixed);
    }

    [Fact]
    public void AnalyzeManyはCollectErrors時に失敗要素を保持して継続する()
    {
        var engine = CreateEngine();

        var results = engine.AnalyzeMany(
            new[] { "UserName", null!, "user_name" },
            failurePolicy: BulkFailurePolicy.CollectErrors);

        Assert.Equal(3, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.False(results[1].IsSuccess);
        Assert.IsType<ArgumentNullException>(results[1].Error);
        Assert.True(results[2].IsSuccess);
        Assert.Equal(CaseStyle.LowerSnakeCase, results[2].Value!.Style);
    }

    [Fact]
    public void AnalyzeManyMemoryは入力順序を維持して結果を返す()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("s_"));

        var results = engine.AnalyzeMany(
            new[] { "UserName".AsMemory(), "s_UserName".AsMemory() },
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("s_"),
            });

        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.Equal(CaseStyle.PascalCase, results[0].Value!.Style);
        Assert.False(results[0].Value.Prefixed);

        Assert.True(results[1].IsSuccess);
        Assert.Equal(CaseStyle.PascalCase, results[1].Value!.Style);
        Assert.True(results[1].Value.Prefixed);
    }

    [Fact]
    public void TryAnalyzeManyは成功可否と結果を要素ごとに返す()
    {
        var engine = CreateEngine();

        var results = engine.TryAnalyzeMany(new[] { "UserName", "m_UserName" });

        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.True(results[0].Value!.Success);
        Assert.Equal(CaseStyle.PascalCase, results[0].Value.Result.Style);

        Assert.True(results[1].IsSuccess);
        Assert.False(results[1].Value!.Success);
        Assert.Equal(CaseStyle.Unknown, results[1].Value.Result.Style);
    }

    [Fact]
    public void TryAnalyzeManyMemoryは成功可否と結果を要素ごとに返す()
    {
        var engine = CreateEngine();

        var results = engine.TryAnalyzeMany(new[] { "UserName".AsMemory(), "m_UserName".AsMemory() });

        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.True(results[0].Value!.Success);
        Assert.Equal(CaseStyle.PascalCase, results[0].Value.Result.Style);

        Assert.True(results[1].IsSuccess);
        Assert.False(results[1].Value!.Success);
        Assert.Equal(CaseStyle.Unknown, results[1].Value.Result.Style);
    }

    [Fact]
    public void ConvertManyMemoryは複数入力を指定Caseへ変換できる()
    {
        var engine = CreateEngine();

        var inputs = new[]
        {
            "UserName".AsMemory(),
            "HTTPServer".AsMemory(),
        };

        var results = engine.ConvertMany(inputs, CaseStyle.CamelCase);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("userName", results[0].Value!.Output);
        Assert.True(results[1].IsSuccess);
        Assert.Equal("httpServer", results[1].Value!.Output);
    }

    [Fact]
    public void InspectManyはCollectErrors時に失敗要素を保持して継続する()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("m"));

        var results = engine.InspectMany(
            new[] { "m_UserName", null!, "__built_in_process__" },
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("m"),
            },
            BulkFailurePolicy.CollectErrors);

        Assert.Equal(3, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("UserName", results[0].Value!.SymbolNameWithoutPrefix);
        Assert.False(results[1].IsSuccess);
        Assert.IsType<ArgumentNullException>(results[1].Error);
        Assert.True(results[2].IsSuccess);
        Assert.Equal(CaseStyle.LowerSnakeCase, results[2].Value!.CaseStyle);
    }

    [Fact]
    public void InspectManyMemoryは入力順序を維持して結果を返す()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("m"));

        var results = engine.InspectMany(
            new[] { "m_UserName".AsMemory(), "__built_in_process__".AsMemory() },
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("m"),
            });

        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("UserName", results[0].Value!.SymbolNameWithoutPrefix);
        Assert.True(results[1].IsSuccess);
        Assert.Equal(CaseStyle.LowerSnakeCase, results[1].Value!.CaseStyle);
    }

    [Fact]
    public void TokenizeManyMemoryは入力順序を維持してトークン化する()
    {
        var engine = CreateEngine(prefixProvider: new TestPrefixProvider("s_"));

        var results = engine.TokenizeMany(new[]
        {
            "s_UserName".AsMemory(),
            "User_Name".AsMemory(),
        });

        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.Equal(TokenCategory.Prefix, results[0].Value![0].Category);
        Assert.True(results[1].IsSuccess);
        Assert.Equal(TokenCategory.Word, results[1].Value![0].Category);
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
