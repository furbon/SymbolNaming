using SymbolNaming.Tokenization;
using SymbolNaming.Tokenization.SplitRules;
using SymbolNaming.Dictionaries;
using SymbolNaming.Tokens;

namespace SymbolNaming.Test;

public class RuleBasedSymbolTokenizerTests
{
    [Fact]
    public void rulesにnull要素を含む場合はArgumentExceptionを送出する()
    {
        Assert.Throws<ArgumentException>(() =>
            new RuleBasedSymbolTokenizer(new ISplitRule[] { new UpperCaseRule(), null! }));
    }

    [Fact]
    public void UnderscoreRuleは範囲外インデックスでNoSplitを返す()
    {
        var rule = new UnderscoreRule();

        var result = rule.Check("_".AsSpan(), 1);

        Assert.False(result.IsSplit);
    }

    [Fact]
    public void VerbatimRuleは範囲外インデックスでNoSplitを返す()
    {
        var rule = new VerbatimRule();

        var result = rule.Check("@".AsSpan(), 1);

        Assert.False(result.IsSplit);
    }

    [Fact]
    public void string入力がnullならArgumentNullExceptionを送出する()
    {
        var tokenizer = new RuleBasedSymbolTokenizer();

        Assert.Throws<ArgumentNullException>(() => tokenizer.Tokenize((string)null!));
    }

    [Fact]
    public void Freeze前にTokenizeするとInvalidOperationExceptionを送出する()
    {
        var tokenizer = new RuleBasedSymbolTokenizer(new[] { new UpperCaseRule() });

        Assert.Throws<InvalidOperationException>(() => tokenizer.Tokenize("UserName"));
    }

    [Fact]
    public void Freeze後にAddRuleするとInvalidOperationExceptionを送出する()
    {
        var tokenizer = new RuleBasedSymbolTokenizer();
        tokenizer.Freeze();

        Assert.Throws<InvalidOperationException>(() => tokenizer.AddRule(new UpperCaseRule()));
    }

    [Fact]
    public void ReadOnlySpan入力でTokenizeするとHasSourceはfalseになる()
    {
        var tokenizer = CreateTokenizer(new UpperCaseRule());

        var tokens = tokenizer.Tokenize("UserName".AsSpan());

        Assert.False(tokens.HasSource);
    }

    [Theory]
    [InlineData("@value", new[] { "@", "value" }, new[] { TokenCategory.Separator, TokenCategory.Word })]
    [InlineData("value", new[] { "value" }, new[] { TokenCategory.Word })]
    public void VerbatimRule_単体で分割できる(string input, string[] expected, TokenCategory[] categories)
    {
        var tokenizer = CreateTokenizer(new VerbatimRule());

        AssertTokenize(tokenizer, input, expected, categories);
    }

    [Theory]
    [InlineData("HTTPRequestMessage", new[] { "HTTP", "Request", "Message" }, new[] { TokenCategory.Word, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("user_id", new[] { "user", "_", "id" }, new[] { TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word })]
    [InlineData("TryGetUTF8Bytes", new[] { "Try", "Get", "UTF8", "Bytes" }, new[] { TokenCategory.Word, TokenCategory.Word, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("_backingField", new[] { "_", "backing", "Field" }, new[] { TokenCategory.Separator, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("__FILE_PATH__", new[] { "_", "_", "FILE", "_", "PATH", "_", "_" }, new[] { TokenCategory.Separator, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Separator, TokenCategory.Separator })]
    public void 実利用に近いシンボルを期待通りに分割できる(string input, string[] expected, TokenCategory[] categories)
    {
        var tokenizer = CreateTokenizer(
            new VerbatimRule(),
            new UpperCaseRule(),
            new PostDigitRule(),
            new UnderscoreRule());

        AssertTokenize(tokenizer, input, expected, categories);
    }

    [Fact]
    public void 先頭語が辞書語かつPrefixProviderにも一致する場合はDictionaryを維持する()
    {
        var tokenizer = new RuleBasedSymbolTokenizer(
            new ISplitRule[] { new UpperCaseRule() },
            new TestProtectedWordProvider("str"),
            new TestPrefixProvider("str"));

        AssertTokenize(
            tokenizer,
            "strUserName",
            new[] { "str", "User", "Name" },
            new[] { TokenCategory.Dictionary, TokenCategory.Word, TokenCategory.Word });
    }

    [Fact]
    public void PrefixProviderで先頭語をPrefixカテゴリにできる()
    {
        var tokenizer = new RuleBasedSymbolTokenizer(
            new ISplitRule[] { new UpperCaseRule() },
            EmptyProtectedWordProvider.Instance,
            new TestPrefixProvider("m"));

        AssertTokenize(
            tokenizer,
            "mTestValue",
            new[] { "m", "Test", "Value" },
            new[] { TokenCategory.Prefix, TokenCategory.Word, TokenCategory.Word });
    }

    [Fact]
    public void PrefixProviderとUnderscoreRuleでm_UserNameを期待通りに分割できる()
    {
        var tokenizer = new RuleBasedSymbolTokenizer(
            new ISplitRule[]
            {
                new UpperCaseRule(),
                new UnderscoreRule(),
            },
            EmptyProtectedWordProvider.Instance,
            new TestPrefixProvider("m"));

        AssertTokenize(
            tokenizer,
            "m_UserName",
            new[] { "m", "_", "User", "Name" },
            new[] { TokenCategory.Prefix, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Word });
    }

    [Theory]
    [InlineData("s_UserName", new[] { "s" }, new[] { "s", "_", "User", "Name" }, new[] { TokenCategory.Prefix, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("s_UserName", new[] { "s_" }, new[] { "s_", "User", "Name" }, new[] { TokenCategory.Prefix, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("s_HTTP2Value", new[] { "s_" }, new[] { "s_", "HTTP2", "Value" }, new[] { TokenCategory.Prefix, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("s_2DModel", new[] { "s_" }, new[] { "s_", "2", "D", "Model" }, new[] { TokenCategory.Prefix, TokenCategory.Word, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("s__UserName", new[] { "s_" }, new[] { "s_", "_", "User", "Name" }, new[] { TokenCategory.Prefix, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Word })]
    public void PrefixProviderでsとs_を登録した場合に期待通りトークナイズできる(
        string input,
        string[] prefixes,
        string[] expected,
        TokenCategory[] categories)
    {
        var tokenizer = new RuleBasedSymbolTokenizer(
            new ISplitRule[]
            {
                new VerbatimRule(),
                new UpperCaseRule(),
                new PostDigitRule(),
                new UnderscoreRule(),
            },
            EmptyProtectedWordProvider.Instance,
            new TestPrefixProvider(prefixes));

        AssertTokenize(tokenizer, input, expected, categories);
    }

    [Fact]
    public void InterfacePrefixと辞書語が競合する場合は辞書語を優先する()
    {
        var tokenizer = new RuleBasedSymbolTokenizer(
            new ISplitRule[] { new UpperCaseRule() },
            new TestProtectedWordProvider("ISBN"),
            new TestPrefixProvider("I"));

        AssertTokenize(
            tokenizer,
            "ISBNCode",
            new[] { "ISBN", "Code" },
            new[] { TokenCategory.Dictionary, TokenCategory.Word });
    }

    [Fact]
    public void ProtectedWordProviderで語をDictionaryカテゴリにできる()
    {
        var tokenizer = new RuleBasedSymbolTokenizer(
            new ISplitRule[] { new UpperCaseRule() },
            new TestProtectedWordProvider("str"),
            EmptyPrefixProvider.Instance);

        AssertTokenize(
            tokenizer,
            "strUserName",
            new[] { "str", "User", "Name" },
            new[] { TokenCategory.Dictionary, TokenCategory.Word, TokenCategory.Word });
    }

    [Fact]
    public void PrefixProviderに複数登録した場合は一致したプレフィックスを先頭語に適用できる()
    {
        var tokenizer = new RuleBasedSymbolTokenizer(
            new ISplitRule[]
            {
                new VerbatimRule(),
                new UpperCaseRule(),
                new PostDigitRule(),
                new UnderscoreRule(),
            },
            EmptyProtectedWordProvider.Instance,
            new TestPrefixProvider("m", "s", "s_", "t_"));

        AssertTokenize(
            tokenizer,
            "mTestValue",
            new[] { "m", "Test", "Value" },
            new[] { TokenCategory.Prefix, TokenCategory.Word, TokenCategory.Word });

        AssertTokenize(
            tokenizer,
            "s_UserName",
            new[] { "s_", "User", "Name" },
            new[] { TokenCategory.Prefix, TokenCategory.Word, TokenCategory.Word });

        AssertTokenize(
            tokenizer,
            "t_Value2D",
            new[] { "t_", "Value2", "D" },
            new[] { TokenCategory.Prefix, TokenCategory.Word, TokenCategory.Word });
    }

    [Fact]
    public void ProtectedWordProviderに複数登録した場合は1つのシンボル内の複数語をDictionaryカテゴリにできる()
    {
        var tokenizer = new RuleBasedSymbolTokenizer(
            new ISplitRule[]
            {
                new UpperCaseRule(),
                new PostDigitRule(),
            },
            new TestProtectedWordProvider("str", "XML", "ID"),
            EmptyPrefixProvider.Instance);

        AssertTokenize(
            tokenizer,
            "strXMLRequestIDValue",
            new[] { "str", "XML", "Request", "ID", "Value" },
            new[] { TokenCategory.Dictionary, TokenCategory.Dictionary, TokenCategory.Word, TokenCategory.Dictionary, TokenCategory.Word });
    }

    [Fact]
    public void PrefixとProtectedWordを複数登録した複合条件でも期待通りトークナイズできる()
    {
        var tokenizer = new RuleBasedSymbolTokenizer(
            new ISplitRule[]
            {
                new VerbatimRule(),
                new UpperCaseRule(),
                new PostDigitRule(),
                new UnderscoreRule(),
            },
            new TestProtectedWordProvider("XML", "ID"),
            new TestPrefixProvider("m", "s", "s_"));

        AssertTokenize(
            tokenizer,
            "s_XML_IDValue",
            new[] { "s_", "XML", "_", "ID", "Value" },
            new[] { TokenCategory.Prefix, TokenCategory.Dictionary, TokenCategory.Separator, TokenCategory.Dictionary, TokenCategory.Word });
    }

    [Theory]
    [InlineData("MyClassTest", new[] { "My", "Class", "Test" })]
    [InlineData("XMLCode", new[] { "XML", "Code" })]
    [InlineData("XML", new[] { "XML" })]
    [InlineData("IServiceProvider", new[] { "I", "Service", "Provider" })]
    [InlineData("ClassDataAttribute", new[] { "Class", "Data", "Attribute" })]
    public void UpperCaseRule_単体で分割できる(string input, string[] expected)
    {
        var tokenizer = CreateTokenizer(new UpperCaseRule());

        AssertTokenize(tokenizer, input, expected, Enumerable.Repeat(TokenCategory.Word, expected.Length).ToArray());
    }

    [Theory]
    [InlineData("Vector3Table", new[] { "Vector3", "Table" })]
    [InlineData("A1B2C", new[] { "A1", "B2", "C" })]
    [InlineData("NoDigit", new[] { "NoDigit" })]
    public void PostDigitRule_単体で分割できる(string input, string[] expected)
    {
        var tokenizer = CreateTokenizer(new PostDigitRule());

        AssertTokenize(tokenizer, input, expected, Enumerable.Repeat(TokenCategory.Word, expected.Length).ToArray());
    }

    [Theory]
    [InlineData("My_Class", new[] { "My", "_", "Class" }, new[] { TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word })]
    [InlineData("A__B", new[] { "A", "_", "_", "B" }, new[] { TokenCategory.Word, TokenCategory.Separator, TokenCategory.Separator, TokenCategory.Word })]
    public void UnderscoreRule_単体で分割できる(string input, string[] expected, TokenCategory[] categories)
    {
        var tokenizer = CreateTokenizer(new UnderscoreRule());

        AssertTokenize(tokenizer, input, expected, categories);
    }

    [Theory]
    [InlineData("MyClass_3Table", new[] { "My", "Class", "_", "3", "Table" }, new[] { TokenCategory.Word, TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("XMLCode_2DModel", new[] { "XML", "Code", "_", "2", "D", "Model" }, new[] { TokenCategory.Word, TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("R3_STICK", new[] { "R3", "_", "STICK" }, new[] { TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word })]
    [InlineData("R_3_STICK", new[] { "R", "_", "3", "_", "STICK" }, new[] { TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word })]
    [InlineData("@Vector3Table_Test", new[] { "@", "Vector3", "Table", "_", "Test" }, new[] { TokenCategory.Separator, TokenCategory.Word, TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word })]
    [InlineData("m_testValue", new[] { "m", "_", "test", "Value" }, new[] { TokenCategory.Word, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("TryParseXMLComment", new[] { "Try", "Parse", "XML", "Comment" }, new[] { TokenCategory.Word, TokenCategory.Word, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("mTestVector3Value", new[] { "m", "Test", "Vector3", "Value" }, new[] { TokenCategory.Word, TokenCategory.Word, TokenCategory.Word, TokenCategory.Word })]
    [InlineData("__InternalValue__", new[] { "_", "_", "Internal", "Value", "_", "_" }, new[] { TokenCategory.Separator, TokenCategory.Separator, TokenCategory.Word, TokenCategory.Word, TokenCategory.Separator, TokenCategory.Separator })]
    public void 複数ルールの組み合わせで分割できる(string input, string[] expected, TokenCategory[] categories)
    {
        var tokenizer = CreateTokenizer(
            new VerbatimRule(),
            new UpperCaseRule(),
            new PostDigitRule(),
            new UnderscoreRule());

        AssertTokenize(tokenizer, input, expected, categories);
    }

    private static RuleBasedSymbolTokenizer CreateTokenizer(params ISplitRule[] rules)
    {
        var tokenizer = new RuleBasedSymbolTokenizer();
        foreach (var splitRule in rules)
        {
            tokenizer.AddRule(splitRule);
        }

        tokenizer.Freeze();
        return tokenizer;
    }

    private static void AssertTokenize(ISymbolTokenizer tokenizer, string input, string[] expected, TokenCategory[] categories)
    {
        if (tokenizer is RuleBasedSymbolTokenizer ruleBasedTokenizer && !ruleBasedTokenizer.IsFrozen)
        {
            ruleBasedTokenizer.Freeze();
        }

        var tokens = tokenizer.Tokenize(input);

        Assert.Equal(expected, tokens.Select(t => t.AsSpan(input).ToString()).ToArray());
        Assert.Equal(categories, tokens.Select(t => t.Category).ToArray());
    }

}
