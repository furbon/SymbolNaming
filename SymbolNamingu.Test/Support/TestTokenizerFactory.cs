using SymbolNaming.Dictionaries;
using SymbolNaming.Tokenization;
using SymbolNaming.Tokenization.SplitRules;

namespace SymbolNaming.Test;

internal static class TestTokenizerFactory
{
    public static RuleBasedSymbolTokenizer CreateDefault(
        IProtectedWordProvider? protectedWordProvider = null,
        IPrefixProvider? prefixProvider = null)
    {
        return new RuleBasedSymbolTokenizer(
            new ISplitRule[]
            {
                new VerbatimRule(),
                new UpperCaseRule(),
                new PostDigitRule(),
                new UnderscoreRule(),
            },
            protectedWordProvider ?? EmptyProtectedWordProvider.Instance,
            prefixProvider ?? EmptyPrefixProvider.Instance);
    }
}
