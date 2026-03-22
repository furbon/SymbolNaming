namespace SymbolNaming.Conversion;

/// <summary>
/// 頭字語の変換方針を表します。
/// </summary>
public enum AcronymPolicy
{
    /// <summary>
    /// 頭字語を可能な限り維持します。
    /// </summary>
    Preserve,

    /// <summary>
    /// 頭字語を通常語として正規化します。
    /// </summary>
    Normalize,
}
