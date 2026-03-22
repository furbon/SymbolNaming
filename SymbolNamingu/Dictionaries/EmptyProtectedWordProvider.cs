namespace SymbolNaming.Dictionaries;

public sealed class EmptyProtectedWordProvider : IProtectedWordProvider
{
    public static EmptyProtectedWordProvider Instance { get; } = new();

    private EmptyProtectedWordProvider()
    {
    }

    public bool IsProtected(ReadOnlySpan<char> text)
    {
        return false;
    }

    public bool TryMatchLongest(ReadOnlySpan<char> text, int start, out int length)
    {
        length = 0;
        return false;
    }
}
