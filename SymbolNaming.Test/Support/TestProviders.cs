using SymbolNaming.Dictionaries;

namespace SymbolNaming.Test;

internal sealed class TestProtectedWordProvider : IProtectedWordProvider
{
    private readonly HashSet<string> _words;

    public TestProtectedWordProvider(params string[] words)
    {
        _words = new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsProtected(ReadOnlySpan<char> text)
    {
        return _words.Contains(text.ToString());
    }

    public bool TryMatchLongest(ReadOnlySpan<char> text, int start, out int length)
    {
        length = 0;
        return false;
    }
}

internal sealed class TestPrefixProvider : IPrefixProvider
{
    private readonly HashSet<string> _prefixes;

    public TestPrefixProvider(params string[] prefixes)
    {
        _prefixes = new HashSet<string>(prefixes, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsPrefix(ReadOnlySpan<char> text)
    {
        return _prefixes.Contains(text.ToString());
    }

    public bool TryMatchLongest(ReadOnlySpan<char> text, int start, out int length)
    {
        if ((uint)start >= (uint)text.Length)
        {
            length = 0;
            return false;
        }

        var slice = text.Slice(start);
        var maxLength = 0;

        foreach (var prefix in _prefixes)
        {
            if (prefix.Length <= maxLength)
            {
                continue;
            }

            if (slice.StartsWith(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                maxLength = prefix.Length;
            }
        }

        length = maxLength;
        return maxLength > 0;
    }
}
