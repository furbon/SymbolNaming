namespace SymbolNaming.Dictionaries;

public sealed class EmptyPrefixProvider : IPrefixProvider
{
    public static EmptyPrefixProvider Instance { get; } = new();

    private EmptyPrefixProvider()
    {
    }

    public bool IsPrefix(ReadOnlySpan<char> text)
    {
        return false;
    }

    public bool TryMatchLongest(ReadOnlySpan<char> text, int start, out int length)
    {
        length = 0;
        return false;
    }
}
