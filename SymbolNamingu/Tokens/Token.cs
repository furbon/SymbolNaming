namespace SymbolNaming.Tokens;

public readonly struct Token
{
    public readonly int Start;
    public readonly int Length;
    public readonly TokenCategory Category;

    public Token(int start, int length, TokenCategory category)
    {
        Start = start;
        Length = length;
        Category = category;
    }

    public ReadOnlySpan<char> AsSpan(ReadOnlySpan<char> input)
    {
        return input.Slice(Start, Length);
    }
}
