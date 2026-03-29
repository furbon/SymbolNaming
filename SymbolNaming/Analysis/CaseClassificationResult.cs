namespace SymbolNaming.Analysis;

/// <summary>
/// Case 分類結果を表します。
/// </summary>
public readonly struct CaseClassificationResult
{
    /// <summary>
    /// 判定不能を表す既定値です。
    /// </summary>
    public static CaseClassificationResult Unknown { get; } = new(CaseStyle.Unknown, prefixed: false, SymbolDecorationInfo.None);

    /// <summary>
    /// 新しい分類結果を初期化します。
    /// </summary>
    public CaseClassificationResult(CaseStyle style, bool prefixed)
        : this(style, prefixed, SymbolDecorationInfo.None)
    {
    }

    /// <summary>
    /// 新しい分類結果を初期化します。
    /// </summary>
    public CaseClassificationResult(CaseStyle style, bool prefixed, SymbolDecorationInfo decoration)
    {
        Style = style;
        Prefixed = prefixed;
        Decoration = decoration;
    }

    /// <summary>
    /// 判定された命名スタイルです。
    /// </summary>
    public CaseStyle Style { get; }

    /// <summary>
    /// プレフィックスが付与されたシンボルであるかを示します。
    /// </summary>
    public bool Prefixed { get; }

    /// <summary>
    /// 先頭/末尾の装飾情報です。
    /// </summary>
    public SymbolDecorationInfo Decoration { get; }
}
