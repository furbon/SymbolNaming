using SymbolNaming.Analysis;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

/// <summary>
/// Span 入力に対する解析結果とプレフィックス分解情報を保持します。
/// </summary>
public readonly ref struct SymbolInspectionSpan
{
    private readonly ReadOnlySpan<char> _source;
    private readonly SymbolInspectionSliceInfo _sliceInfo;

    internal SymbolInspectionSpan(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification, IReadOnlyList<SymbolInspectionWarning> warnings)
    {
        _source = source;
        Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        Classification = classification;
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
        _sliceInfo = SymbolInspectionSliceInfoFactory.Create(source, tokens, classification);
    }

    /// <summary>
    /// 入力元 Span です。
    /// </summary>
    public ReadOnlySpan<char> Source => _source;

    /// <summary>
    /// トークン列です。
    /// </summary>
    public TokenList Tokens { get; }

    /// <summary>
    /// Case 分類結果です。
    /// </summary>
    public CaseClassificationResult Classification { get; }

    /// <summary>
    /// 判定された命名スタイルです。
    /// </summary>
    public CaseStyle CaseStyle => Classification.Style;

    /// <summary>
    /// プレフィックス付きとして判定されたかを示します。
    /// </summary>
    public bool Prefixed => Classification.Prefixed;

    /// <summary>
    /// プレフィックスが抽出できたかを示します。
    /// </summary>
    public bool HasPrefix => _sliceInfo.PrefixLength > 0;

    /// <summary>
    /// 抽出されたプレフィックス Span です。
    /// </summary>
    public ReadOnlySpan<char> Prefix =>
        HasPrefix
            ? _source.Slice(_sliceInfo.PrefixStart, _sliceInfo.PrefixLength)
            : ReadOnlySpan<char>.Empty;

    /// <summary>
    /// プレフィックスを除去したシンボル名 Span です。
    /// </summary>
    public ReadOnlySpan<char> SymbolNameWithoutPrefix => _source.Slice(_sliceInfo.SymbolStart);

    /// <summary>
    /// 検査時に検出された注意事項の一覧です。
    /// </summary>
    public IReadOnlyList<SymbolInspectionWarning> Warnings { get; }

    /// <summary>
    /// 注意事項が 1 件以上存在するかを示します。
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;
}
