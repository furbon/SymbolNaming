namespace SymbolNaming.Analysis;

/// <summary>
/// シンボル判定時に検出した装飾情報を表します。
/// </summary>
public readonly struct SymbolDecorationInfo
{
    /// <summary>
    /// 装飾なしを表す既定値です。
    /// </summary>
    public static SymbolDecorationInfo None { get; } = new(leadingUnderscoreCount: 0, trailingUnderscoreCount: 0);

    /// <summary>
    /// 新しい装飾情報を初期化します。
    /// </summary>
    public SymbolDecorationInfo(int leadingUnderscoreCount, int trailingUnderscoreCount)
    {
        LeadingUnderscoreCount = leadingUnderscoreCount;
        TrailingUnderscoreCount = trailingUnderscoreCount;
    }

    /// <summary>
    /// 先頭連続アンダースコア数です。
    /// </summary>
    public int LeadingUnderscoreCount { get; }

    /// <summary>
    /// 末尾連続アンダースコア数です。
    /// </summary>
    public int TrailingUnderscoreCount { get; }

    /// <summary>
    /// 先頭アンダースコア装飾を持つかを示します。
    /// </summary>
    public bool HasLeadingUnderscore => LeadingUnderscoreCount > 0;

    /// <summary>
    /// 末尾アンダースコア装飾を持つかを示します。
    /// </summary>
    public bool HasTrailingUnderscore => TrailingUnderscoreCount > 0;
}
