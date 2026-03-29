namespace SymbolNaming.Conversion;

/// <summary>
/// Case 変換結果を表します。
/// </summary>
public sealed class CaseConversionResult
{
    /// <summary>
    /// 変換後の文字列です。
    /// </summary>
    public string Output { get; }

    /// <summary>
    /// 実行時に適用したプレフィックス方針です。
    /// </summary>
    public PrefixPolicy AppliedPrefixPolicy { get; }

    /// <summary>
    /// 実行時に適用した頭字語方針です。
    /// </summary>
    public AcronymPolicy AppliedAcronymPolicy { get; }

    /// <summary>
    /// 変換時に発生した診断情報です。
    /// </summary>
    public IReadOnlyList<CaseConversionWarning> Warnings { get; }

    /// <summary>
    /// 変換結果を初期化します。
    /// </summary>
    public CaseConversionResult(
        string output,
        PrefixPolicy appliedPrefixPolicy,
        AcronymPolicy appliedAcronymPolicy,
        IReadOnlyList<CaseConversionWarning>? warnings = null)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        AppliedPrefixPolicy = appliedPrefixPolicy;
        AppliedAcronymPolicy = appliedAcronymPolicy;
        Warnings = warnings is { Count: > 0 } ? warnings : Array.Empty<CaseConversionWarning>();
    }
}
