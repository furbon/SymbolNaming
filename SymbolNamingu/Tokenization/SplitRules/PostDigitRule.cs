namespace SymbolNaming.Tokenization.SplitRules;

public sealed class PostDigitRule : ISplitRule
{
    public SplitResult Check(ReadOnlySpan<char> span, int index)
    {
        if ((uint)index >= (uint)span.Length || index == 0)
        {
            return SplitResult.NoSplit;
        }

        if (char.IsDigit(span[index - 1]) && !char.IsDigit(span[index]))
        {
            return SplitResult.WordSplit();
        }

        return SplitResult.NoSplit;
    }
}
