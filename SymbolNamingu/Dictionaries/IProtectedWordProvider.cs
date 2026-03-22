namespace SymbolNaming.Dictionaries;

public interface IProtectedWordProvider
{
    bool IsProtected(ReadOnlySpan<char> text);

    bool TryMatchLongest(ReadOnlySpan<char> text, int start, out int length);
}
