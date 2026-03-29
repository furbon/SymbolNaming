using SymbolNaming.Dictionaries;
using SymbolNaming.Tokenization;

namespace SymbolNaming.Test;

internal static class TestTokenizerFactory
{
    public static RuleBasedSymbolTokenizer CreateDefault(
        IProtectedWordProvider? protectedWordProvider = null,
        IPrefixProvider? prefixProvider = null)
    {
        return SymbolTokenizerFactory.CreateDefault(protectedWordProvider, prefixProvider);
    }
}
