namespace SymbolNaming.Dictionaries;

public interface IPrefixProvider
{
    bool IsPrefix(ReadOnlySpan<char> text);

    bool TryMatchLongest(ReadOnlySpan<char> text, int start, out int length);
}
