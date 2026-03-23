namespace SymbolNaming.Engine;

/// <summary>
/// Inspect 時に検出された注意事項を表します。
/// </summary>
public readonly struct SymbolInspectionWarning
{
    /// <summary>
    /// 注意事項を初期化します。
    /// </summary>
    public SymbolInspectionWarning(SymbolInspectionWarningKind kind, int start, int length)
    {
        Kind = kind;
        Start = start;
        Length = length;
    }

    /// <summary>
    /// 注意事項の種類です。
    /// </summary>
    public SymbolInspectionWarningKind Kind { get; }

    /// <summary>
    /// 元文字列内の開始位置です。
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// 注意対象の長さです。
    /// </summary>
    public int Length { get; }
}
