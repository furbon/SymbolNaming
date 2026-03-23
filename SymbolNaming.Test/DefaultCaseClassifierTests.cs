using SymbolNaming.Analysis;
using SymbolNaming.Tokens;

namespace SymbolNaming.Test;

public class DefaultCaseClassifierTests
{
    [Fact]
    public void tokensがnullならArgumentNullExceptionを送出する()
    {
        var classifier = new DefaultCaseClassifier();

        Assert.Throws<ArgumentNullException>(() => classifier.TryClassify(null!, out _));
    }

    [Fact]
    public void 空トークンはUnknownを返す()
    {
        var classifier = new DefaultCaseClassifier();
        var tokens = new TokenList(Array.Empty<Token>(), string.Empty);

        var success = classifier.TryClassify(tokens, out var result);

        Assert.False(success);
        Assert.Equal(CaseStyle.Unknown, result.Style);
        Assert.False(result.Prefixed);
    }

    [Fact]
    public void UNKNOWNは単一トークンでもScreamingSnakeCaseを判定できる()
    {
        var tokenizer = TestTokenizerFactory.CreateDefault();
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("UNKNOWN");

        var success = classifier.TryClassify(tokens, out var result);

        Assert.True(success);
        Assert.Equal(CaseStyle.ScreamingSnakeCase, result.Style);
        Assert.False(result.Prefixed);
    }

    [Fact]
    public void セパレーターなし単一トークン_PlayerはPascalCaseとUpperSnakeCaseに適合する()
    {
        var tokenizer = TestTokenizerFactory.CreateDefault();
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("Player");

        var matches = classifier.GetMatches(tokens);

        Assert.True(matches.PascalCase);
        Assert.True(matches.UpperSnakeCase);
        Assert.False(matches.CamelCase);
        Assert.False(matches.LowerSnakeCase);
        Assert.False(matches.ScreamingSnakeCase);
    }

    [Fact]
    public void セパレーターなし単一トークン_enemyはCamelCaseとLowerSnakeCaseに適合する()
    {
        var tokenizer = TestTokenizerFactory.CreateDefault();
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("enemy");

        var matches = classifier.GetMatches(tokens);

        Assert.False(matches.PascalCase);
        Assert.True(matches.CamelCase);
        Assert.False(matches.UpperSnakeCase);
        Assert.True(matches.LowerSnakeCase);
        Assert.False(matches.ScreamingSnakeCase);
    }

    [Theory]
    [InlineData("Player", CaseStyle.UpperSnakeCase)]
    [InlineData("enemy", CaseStyle.LowerSnakeCase)]
    public void 単一トークン曖昧判定はPreferSnakeCaseでスネークケースを優先できる(string input, CaseStyle expected)
    {
        var tokenizer = TestTokenizerFactory.CreateDefault();
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize(input);

        var success = classifier.TryClassify(
            tokens,
            out var result,
            new CaseAnalysisOptions
            {
                AmbiguousSingleTokenPolicy = AmbiguousSingleTokenPolicy.PreferSnakeCase,
            });

        Assert.True(success);
        Assert.Equal(expected, result.Style);
    }

    [Fact]
    public void 単一トークン曖昧判定はReturnUnknownでUnknownを返せる()
    {
        var tokenizer = TestTokenizerFactory.CreateDefault();
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("Player");

        var success = classifier.TryClassify(
            tokens,
            out var result,
            new CaseAnalysisOptions
            {
                AmbiguousSingleTokenPolicy = AmbiguousSingleTokenPolicy.ReturnUnknown,
            });

        Assert.False(success);
        Assert.Equal(CaseStyle.Unknown, result.Style);
    }

    [Fact]
    public void 単一トークン曖昧判定はCustomResolverで最終スタイルを制御できる()
    {
        var tokenizer = TestTokenizerFactory.CreateDefault();
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("Player");

        var success = classifier.TryClassify(
            tokens,
            out var result,
            new CaseAnalysisOptions
            {
                AmbiguousSingleTokenPolicy = AmbiguousSingleTokenPolicy.UseCustomResolver,
                AmbiguousSingleTokenResolver = matches => matches.UpperSnakeCase ? CaseStyle.UpperSnakeCase : (CaseStyle?)null,
            });

        Assert.True(success);
        Assert.Equal(CaseStyle.UpperSnakeCase, result.Style);
    }

    [Theory]
    [InlineData("s")]
    [InlineData("s_")]
    public void Prefix接続アンダースコア_単一語_UNKNOWNはScreamingSnakeCaseかつPrefixedになる(string prefix)
    {
        var tokenizer = TestTokenizerFactory.CreateDefault(prefixProvider: new TestPrefixProvider(prefix));
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("s_UNKNOWN");

        var success = classifier.TryClassify(
            tokens,
            out var result,
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider(prefix),
            });

        Assert.True(success);
        Assert.Equal(CaseStyle.ScreamingSnakeCase, result.Style);
        Assert.True(result.Prefixed);
    }

    [Fact]
    public void sourceなしトークンはUnknownを返す()
    {
        var classifier = new DefaultCaseClassifier();
        var tokens = new TokenList(new[] { new Token(0, 4, TokenCategory.Word) });

        var success = classifier.TryClassify(tokens, out var result);

        Assert.False(success);
        Assert.Equal(CaseStyle.Unknown, result.Style);
        Assert.False(result.Prefixed);
    }

    [Fact]
    public void PrefixProviderでPascalCaseかつPrefixedを判定できる()
    {
        var classifier = new DefaultCaseClassifier();
        const string input = "mTestValue";
        var tokens = new TokenList(new[]
        {
            new Token(0, 1, TokenCategory.Prefix),
            new Token(1, 4, TokenCategory.Word),
            new Token(5, 5, TokenCategory.Word),
        }, input);

        var success = classifier.TryClassify(
            tokens,
            out var classification,
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("m"),
            });

        Assert.True(success);
        Assert.Equal(CaseStyle.PascalCase, classification.Style);
        Assert.True(classification.Prefixed);
    }

    [Fact]
    public void 辞書語はプレフィックス扱いせずcamelCaseを判定できる()
    {
        var classifier = new DefaultCaseClassifier();
        const string input = "strUserName";
        var tokens = new TokenList(new[]
        {
            new Token(0, 3, TokenCategory.Dictionary),
            new Token(3, 4, TokenCategory.Word),
            new Token(7, 4, TokenCategory.Word),
        }, input);

        var success = classifier.TryClassify(
            tokens,
            out var classification,
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("str"),
                ProtectedWordProvider = new TestProtectedWordProvider("str"),
            });

        Assert.True(success);
        Assert.Equal(CaseStyle.CamelCase, classification.Style);
        Assert.False(classification.Prefixed);
    }

    [Fact]
    public void 通常のcamelCaseを判定できる()
    {
        var classifier = new DefaultCaseClassifier();
        const string input = "testValue";
        var tokens = new TokenList(new[]
        {
            new Token(0, 4, TokenCategory.Word),
            new Token(4, 5, TokenCategory.Word),
        }, input);

        var success = classifier.TryClassify(tokens, out var result);

        Assert.True(success);
        Assert.Equal(CaseStyle.CamelCase, result.Style);
        Assert.False(result.Prefixed);
    }

    [Theory]
    [InlineData("UserName", CaseStyle.PascalCase, false)]
    [InlineData("userName", CaseStyle.CamelCase, false)]
    [InlineData("user_name", CaseStyle.LowerSnakeCase, false)]
    [InlineData("my_snake_case", CaseStyle.LowerSnakeCase, false)]
    [InlineData("My_Test_Value", CaseStyle.UpperSnakeCase, false)]
    [InlineData("USER_NAME", CaseStyle.ScreamingSnakeCase, false)]
    [InlineData("TryParseXMLComment", CaseStyle.PascalCase, false)]
    [InlineData("vector3Table", CaseStyle.CamelCase, false)]
    [InlineData("m_UserName", CaseStyle.Unknown, false)]
    public void 実利用に近い入力のCaseStyleとPrefixedを判定できる(string input, CaseStyle expectedStyle, bool expectedPrefixed)
    {
        var tokenizer = TestTokenizerFactory.CreateDefault();
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize(input);

        var success = classifier.TryClassify(tokens, out var result);

        if (expectedStyle == CaseStyle.Unknown)
        {
            Assert.False(success);
            Assert.Equal(CaseStyle.Unknown, result.Style);
            Assert.False(result.Prefixed);
            return;
        }

        Assert.True(success);
        Assert.Equal(expectedStyle, result.Style);
        Assert.Equal(expectedPrefixed, result.Prefixed);
    }

    [Fact]
    public void ISBNCodeは辞書語を優先してInterfaceプレフィックスと誤判定しない()
    {
        var tokenizer = TestTokenizerFactory.CreateDefault(
            protectedWordProvider: new TestProtectedWordProvider("ISBN"),
            prefixProvider: new TestPrefixProvider("I"));
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("ISBNCode");

        Assert.Equal(new[] { TokenCategory.Dictionary, TokenCategory.Word }, tokens.Select(t => t.Category).ToArray());

        var success = classifier.TryClassify(
            tokens,
            out var classification,
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("I"),
                ProtectedWordProvider = new TestProtectedWordProvider("ISBN"),
            });

        Assert.True(success);
        Assert.Equal(CaseStyle.PascalCase, classification.Style);
        Assert.False(classification.Prefixed);
    }

    [Fact]
    public void PrefixProviderを使ったs_DATA_VALUEはScreamingSnakeCaseかつPrefixedになる()
    {
        var tokenizer = TestTokenizerFactory.CreateDefault(prefixProvider: new TestPrefixProvider("s"));
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("s_DATA_VALUE");

        var success = classifier.TryClassify(
            tokens,
            out var result,
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("s"),
            });

        Assert.True(success);
        Assert.Equal(CaseStyle.ScreamingSnakeCase, result.Style);
        Assert.True(result.Prefixed);
    }

    [Fact]
    public void PrefixProviderを使ったm_UserNameはPascalCaseかつPrefixedになる()
    {
        var tokenizer = TestTokenizerFactory.CreateDefault(prefixProvider: new TestPrefixProvider("m"));
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("m_UserName");

        var success = classifier.TryClassify(
            tokens,
            out var result,
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("m"),
            });

        Assert.True(success);
        Assert.Equal(CaseStyle.PascalCase, result.Style);
        Assert.True(result.Prefixed);
    }

    [Fact]
    public void PrefixProviderをsで登録したs_UserNameはPascalCaseかつPrefixedになる()
    {
        var tokenizer = TestTokenizerFactory.CreateDefault(prefixProvider: new TestPrefixProvider("s"));
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("s_UserName");

        Assert.Equal(new[] { TokenCategory.Prefix, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Word }, tokens.Select(t => t.Category).ToArray());

        var success = classifier.TryClassify(
            tokens,
            out var result,
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("s"),
            });

        Assert.True(success);
        Assert.Equal(CaseStyle.PascalCase, result.Style);
        Assert.True(result.Prefixed);
    }

    [Fact]
    public void PrefixProviderをs_で登録したs_UserNameはPascalCaseかつPrefixedになる()
    {
        var tokenizer = TestTokenizerFactory.CreateDefault(prefixProvider: new TestPrefixProvider("s_"));
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("s_UserName");

        Assert.Equal(new[] { TokenCategory.Prefix, TokenCategory.Word, TokenCategory.Word }, tokens.Select(t => t.Category).ToArray());

        var success = classifier.TryClassify(
            tokens,
            out var result,
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider("s_"),
            });

        Assert.True(success);
        Assert.Equal(CaseStyle.PascalCase, result.Style);
        Assert.True(result.Prefixed);
    }

    [Theory]
    [InlineData("s", new[] { TokenCategory.Prefix, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word })]
    [InlineData("s_", new[] { TokenCategory.Prefix, TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word })]
    public void s_User_Nameはprefix設定がsでもs_でもUpperSnakeCaseかつPrefixedになる(string prefix, TokenCategory[] expectedCategories)
    {
        var tokenizer = TestTokenizerFactory.CreateDefault(prefixProvider: new TestPrefixProvider(prefix));
        var classifier = new DefaultCaseClassifier();
        var tokens = tokenizer.Tokenize("s_User_Name");

        Assert.Equal(expectedCategories, tokens.Select(t => t.Category).ToArray());

        var success = classifier.TryClassify(
            tokens,
            out var result,
            new CaseAnalysisOptions
            {
                PrefixProvider = new TestPrefixProvider(prefix),
            });

        Assert.True(success);
        Assert.Equal(CaseStyle.UpperSnakeCase, result.Style);
        Assert.True(result.Prefixed);
    }

}
