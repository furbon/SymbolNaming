using SymbolNaming.Dictionaries;
using SymbolNaming.Tokenization;

namespace SymbolNaming.Tests;

internal static class TestTokenizerFactory
{
    public static RuleBasedSymbolTokenizer CreateDefault(
        IProtectedWordProvider? protectedWordProvider = null,
        IPrefixProvider? prefixProvider = null)
    {
        return SymbolTokenizerFactory.CreateDefault(protectedWordProvider, prefixProvider);
    }
}
