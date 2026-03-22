namespace SymbolNaming.Tokenization.SplitRules;

public sealed class UnderscoreRule : ISplitRule
{
    public SplitResult Check(ReadOnlySpan<char> span, int index)
    {
        if ((uint)index >= (uint)span.Length)
        {
            return SplitResult.NoSplit;
        }

        if (span[index] == '_')
        {
            return SplitResult.SeparatorSplit();
        }

        return SplitResult.NoSplit;
    }
}
