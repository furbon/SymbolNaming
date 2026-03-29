namespace SymbolNaming.Conversion;

/// <summary>
/// Case 変換時の診断情報を表します。
/// </summary>
public enum CaseConversionWarning
{
    /// <summary>
    /// 変換対象となる単語トークンが存在しませんでした。
    /// </summary>
    NoWordToken,

    /// <summary>
    /// <see cref="PrefixPolicy.Add"/> 指定時に付与プレフィックスが空でした。
    /// </summary>
    EmptyPrefixToAdd,
}
