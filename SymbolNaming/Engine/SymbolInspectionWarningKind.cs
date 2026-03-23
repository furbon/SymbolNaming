namespace SymbolNaming.Engine;

/// <summary>
/// Inspect 時に検出される注意事項の種類です。
/// </summary>
public enum SymbolInspectionWarningKind
{
    /// <summary>
    /// 先頭に 1 文字の大文字語が現れ、意図しない分割の可能性があります。
    /// 例: <c>PLayer</c>。
    /// </summary>
    SuspiciousLeadingSingleUpperToken,
}
