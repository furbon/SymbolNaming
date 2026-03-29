using SymbolNaming.Dictionaries;
using SymbolNaming.Tokenization;
using SymbolNaming.Tokenization.SplitRules;
using SymbolNaming.Tokens;

namespace SymbolNaming.Tests;

public class ProviderUtilityTests
{
    [Fact]
    public void ProtectedWordSetProviderは大文字小文字を無視して一致判定できる()
    {
        var provider = new ProtectedWordSetProvider("XML", "ID");

        Assert.True(provider.IsProtected("xml"));
        Assert.True(provider.IsProtected("Id"));
        Assert.False(provider.IsProtected("User"));
    }

    [Fact]
    public void PrefixSetProviderは最長一致を返せる()
    {
        var provider = new PrefixSetProvider("s", "s_");

        var matched = provider.TryMatchLongest("s_UserName", 0, out var length);

        Assert.True(matched);
        Assert.Equal(2, length);
    }

    [Fact]
    public void SymbolTokenizerFactoryはFreeze済み既定トークナイザーを返す()
    {
        var tokenizer = SymbolTokenizerFactory.CreateDefault(
            prefixProvider: new PrefixSetProvider("s_"));

        Assert.True(tokenizer.IsFrozen);

        var tokens = tokenizer.Tokenize("s_UserName");

        Assert.Equal(new[] { "s_", "User", "Name" }, tokens.Select(x => tokens.GetSpan(x).ToString()).ToArray());
        Assert.Equal(new[] { TokenCategory.Prefix, TokenCategory.Word, TokenCategory.Word }, tokens.Select(x => x.Category).ToArray());
    }

    [Fact]
    public void SymbolTokenizerFactoryは既定ルールを既定順で返す()
    {
        var rules = SymbolTokenizerFactory.CreateDefaultRules();

        Assert.Collection(
            rules,
            x => Assert.IsType<VerbatimRule>(x),
            x => Assert.IsType<UpperCaseRule>(x),
            x => Assert.IsType<PostDigitRule>(x),
            x => Assert.IsType<UnderscoreRule>(x));
    }
}
