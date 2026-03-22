namespace SymbolNaming.Tokens;

/// <summary>
/// トークンの分類を表します。
/// </summary>
public enum TokenCategory
{
    /// <summary>
    /// 通常の語トークンです。
    /// </summary>
    Word,

    /// <summary>
    /// 区切り記号トークンです。
    /// </summary>
    Separator,

    /// <summary>
    /// 保護語（辞書語）として扱われるトークンです。
    /// </summary>
    Dictionary,

    /// <summary>
    /// プレフィックスとして扱われるトークンです。
    /// </summary>
    Prefix,
}
