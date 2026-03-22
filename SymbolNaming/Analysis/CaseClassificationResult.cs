namespace SymbolNaming.Analysis;

/// <summary>
/// Case 分類結果を表します。
/// </summary>
public readonly struct CaseClassificationResult
{
    /// <summary>
    /// 判定不能を表す既定値です。
    /// </summary>
    public static CaseClassificationResult Unknown { get; } = new(CaseStyle.Unknown, prefixed: false);

    /// <summary>
    /// 新しい分類結果を初期化します。
    /// </summary>
    public CaseClassificationResult(CaseStyle style, bool prefixed)
    {
        Style = style;
        Prefixed = prefixed;
    }

    /// <summary>
    /// 判定された命名スタイルです。
    /// </summary>
    public CaseStyle Style { get; }

    /// <summary>
    /// プレフィックスが付与されたシンボルであるかを示します。
    /// </summary>
    public bool Prefixed { get; }
}
