namespace SymbolNaming.Conversion;

/// <summary>
/// 変換時のプレフィックス扱いを表します。
/// </summary>
public enum PrefixPolicy
{
    /// <summary>
    /// 既存プレフィックスを維持します。
    /// </summary>
    Keep,

    /// <summary>
    /// プレフィックスを除去します。
    /// </summary>
    Remove,

    /// <summary>
    /// 指定プレフィックスを付与します。
    /// </summary>
    Add,
}
