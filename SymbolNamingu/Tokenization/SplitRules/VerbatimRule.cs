namespace SymbolNaming.Tokenization.SplitRules;

public sealed class VerbatimRule : ISplitRule
{
    public SplitResult Check(ReadOnlySpan<char> span, int index)
    {
        if ((uint)index >= (uint)span.Length)
        {
            return SplitResult.NoSplit;
        }

        if (index == 0 && span[index] == '@')
        {
            return SplitResult.SeparatorSplit();
        }

        return SplitResult.NoSplit;
    }
}
