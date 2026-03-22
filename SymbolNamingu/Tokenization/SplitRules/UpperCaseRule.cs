namespace SymbolNaming.Tokenization.SplitRules;

/// <summary>
/// 大文字境界で分割するルールです。
/// </summary>
public sealed class UpperCaseRule : ISplitRule
{
    /// <summary>
    /// 指定位置が大文字境界のとき語分割を返します。
    /// </summary>
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
