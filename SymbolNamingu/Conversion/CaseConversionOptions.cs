namespace SymbolNaming.Conversion;

/// <summary>
/// Case 変換時のオプションを表します。
/// </summary>
public sealed class CaseConversionOptions
{
    /// <summary>
    /// 変換時のプレフィックス方針です。
    /// </summary>
    public PrefixPolicy PrefixPolicy { get; set; } = PrefixPolicy.Keep;

    /// <summary>
    /// <see cref="PrefixPolicy.Add"/> の場合に付与するプレフィックスです。
    /// </summary>
    public string? PrefixToAdd { get; set; }

    /// <summary>
    /// 頭字語の変換方針です。
    /// </summary>
    public AcronymPolicy AcronymPolicy { get; set; } = AcronymPolicy.Preserve;
}
