namespace SymbolNaming.Tokenization.SplitRules;

/// <summary>
/// C# の verbatim 識別子接頭辞 <c>@</c> を区切りとして扱うルールです。
/// </summary>
public sealed class VerbatimRule : ISplitRule
{
    /// <summary>
    /// 先頭が <c>@</c> のとき区切り分割を返します。
    /// </summary>
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
