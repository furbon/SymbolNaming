namespace SymbolNaming.Tokenization.SplitRules;

/// <summary>
/// 数字の直後で非数字へ遷移する境界を分割するルールです。
/// </summary>
public sealed class PostDigitRule : ISplitRule
{
    /// <summary>
    /// 指定位置が数字後境界のとき語分割を返します。
    /// </summary>
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
