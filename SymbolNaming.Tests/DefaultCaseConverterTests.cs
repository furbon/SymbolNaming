using SymbolNaming.Conversion;
using SymbolNaming.Tokens;

namespace SymbolNaming.Tests;

public class DefaultCaseConverterTests
{
    [Fact]
    public void tokensがnullならArgumentNullExceptionを送出する()
    {
        var converter = new DefaultCaseConverter();

        Assert.Throws<ArgumentNullException>(() => converter.Convert(null!, CaseStyle.CamelCase));
    }

    [Fact]
    public void sourceなしトークンはInvalidOperationExceptionを送出する()
    {
        var converter = new DefaultCaseConverter();
        var tokens = new TokenList(new[] { new Token(0, 4, TokenCategory.Word) });

        Assert.Throws<InvalidOperationException>(() => converter.Convert(tokens, CaseStyle.CamelCase));
    }

    [Theory]
    [InlineData("UserName", CaseStyle.CamelCase, "userName")]
    [InlineData("userName", CaseStyle.PascalCase, "UserName")]
    [InlineData("userName", CaseStyle.LowerSnakeCase, "user_name")]
    [InlineData("userName", CaseStyle.UpperSnakeCase, "User_Name")]
    [InlineData("userName", CaseStyle.ScreamingSnakeCase, "USER_NAME")]
    public void 基本的なCase変換ができる(string input, CaseStyle targetStyle, string expected)
    {
        var converter = new DefaultCaseConverter();
        var tokenizer = TestTokenizerFactory.CreateDefault();
        var tokens = tokenizer.Tokenize(input);

        var actual = converter.Convert(tokens, targetStyle);

        Assert.Equal(expected, actual.Output);
    }

    [Fact]
    public void PrefixPolicy_Keepで既存Prefixを維持できる()
    {
        var converter = new DefaultCaseConverter();
        var tokenizer = TestTokenizerFactory.CreateDefault(prefixProvider: new TestPrefixProvider("m"));
        var tokens = tokenizer.Tokenize("m_UserName");

        var actual = converter.Convert(
            tokens,
            CaseStyle.LowerSnakeCase,
            new CaseConversionOptions
            {
                PrefixPolicy = PrefixPolicy.Keep,
            });

        Assert.Equal("m_user_name", actual.Output);
        Assert.Equal(PrefixPolicy.Keep, actual.AppliedPrefixPolicy);
    }

    [Fact]
    public void PrefixPolicy_Removeで既存Prefixを除去できる()
    {
        var converter = new DefaultCaseConverter();
        var tokenizer = TestTokenizerFactory.CreateDefault(prefixProvider: new TestPrefixProvider("m"));
        var tokens = tokenizer.Tokenize("m_UserName");

        var actual = converter.Convert(
            tokens,
            CaseStyle.LowerSnakeCase,
            new CaseConversionOptions
            {
                PrefixPolicy = PrefixPolicy.Remove,
            });

        Assert.Equal("user_name", actual.Output);
    }

    [Fact]
    public void PrefixPolicy_AddでPrefixを付与できる()
    {
        var converter = new DefaultCaseConverter();
        var tokenizer = TestTokenizerFactory.CreateDefault();
        var tokens = tokenizer.Tokenize("UserName");

        var actual = converter.Convert(
            tokens,
            CaseStyle.CamelCase,
            new CaseConversionOptions
            {
                PrefixPolicy = PrefixPolicy.Add,
                PrefixToAdd = "m_",
            });

        Assert.Equal("m_userName", actual.Output);
    }

    [Fact]
    public void AcronymPolicyでPreserveとNormalizeを切り替えできる()
    {
        var converter = new DefaultCaseConverter();
        var tokenizer = TestTokenizerFactory.CreateDefault();
        var tokens = tokenizer.Tokenize("XMLHttpRequest");

        var preserved = converter.Convert(
            tokens,
            CaseStyle.PascalCase,
            new CaseConversionOptions
            {
                AcronymPolicy = AcronymPolicy.Preserve,
            });

        var normalized = converter.Convert(
            tokens,
            CaseStyle.PascalCase,
            new CaseConversionOptions
            {
                AcronymPolicy = AcronymPolicy.Normalize,
            });

        Assert.Equal("XMLHttpRequest", preserved.Output);
        Assert.Equal("XmlHttpRequest", normalized.Output);
        Assert.Equal(AcronymPolicy.Preserve, preserved.AppliedAcronymPolicy);
        Assert.Equal(AcronymPolicy.Normalize, normalized.AppliedAcronymPolicy);
    }

    [Fact]
    public void PrefixPolicy_AddでPrefixToAddが空なら警告を返す()
    {
        var converter = new DefaultCaseConverter();
        var tokenizer = TestTokenizerFactory.CreateDefault();
        var tokens = tokenizer.Tokenize("UserName");

        var result = converter.Convert(
            tokens,
            CaseStyle.CamelCase,
            new CaseConversionOptions
            {
                PrefixPolicy = PrefixPolicy.Add,
                PrefixToAdd = string.Empty,
            });

        Assert.Contains(CaseConversionWarning.EmptyPrefixToAdd, result.Warnings);
    }
}
