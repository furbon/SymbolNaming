namespace SymbolNaming.Tokenization.SplitRules;

/// <summary>
/// アンダースコアを区切りとして分割するルールです。
/// </summary>
public sealed class UnderscoreRule : ISplitRule
{
    /// <summary>
    /// 指定位置がアンダースコアのとき区切り分割を返します。
    /// </summary>
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
