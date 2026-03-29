using SymbolNaming.Dictionaries;

namespace SymbolNaming.Tests;

internal sealed class TestProtectedWordProvider : ProtectedWordProviderBase
{
    private readonly string[] _words;

    public TestProtectedWordProvider(params string[] words)
    {
        _words = words;
    }

    protected override IEnumerable<string> GetProtectedWords()
    {
        return _words;
    }
}

internal sealed class TestPrefixProvider : PrefixProviderBase
{
    private readonly string[] _prefixes;

    public TestPrefixProvider(params string[] prefixes)
    {
        _prefixes = prefixes;
    }

    protected override IEnumerable<string> GetPrefixes()
    {
        return _prefixes;
    }
}
