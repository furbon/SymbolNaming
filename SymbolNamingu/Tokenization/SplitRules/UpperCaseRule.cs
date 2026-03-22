namespace SymbolNaming.Tokenization.SplitRules;

public sealed class UpperCaseRule : ISplitRule
{
    public SplitResult Check(ReadOnlySpan<char> span, int index)
    {
        if ((uint)index >= (uint)span.Length || index == 0)
        {
            return SplitResult.NoSplit;
        }

        var current = span[index];
        if (!char.IsUpper(current))
        {
            return SplitResult.NoSplit;
        }

        var prev = span[index - 1];

        if (char.IsLower(prev))
        {
            return SplitResult.WordSplit();
        }

        if (char.IsUpper(prev) &&
            index + 1 < span.Length &&
            char.IsLower(span[index + 1]))
        {
            return SplitResult.WordSplit();
        }

        return SplitResult.NoSplit;
    }
}
