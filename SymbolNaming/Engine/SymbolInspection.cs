using SymbolNaming.Analysis;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

/// <summary>
/// 文字列入力に対する解析結果とプレフィックス分解情報を保持します。
/// </summary>
public sealed class SymbolInspection
{
    /// <summary>
    /// 新しい検査結果を初期化します。
    /// </summary>
    public SymbolInspection(string source, TokenList tokens, CaseClassificationResult classification)
        : this(source, tokens, classification, Array.Empty<SymbolInspectionWarning>())
    {
    }

    internal SymbolInspection(string source, TokenList tokens, CaseClassificationResult classification, IReadOnlyList<SymbolInspectionWarning> warnings)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        Classification = classification;
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));

        var sliceInfo = SymbolInspectionSliceInfoFactory.Create(source.AsSpan(), tokens, classification);
        HasPrefix = sliceInfo.PrefixLength > 0;
        Prefix = HasPrefix ? source.Substring(sliceInfo.PrefixStart, sliceInfo.PrefixLength) : null;
        SymbolNameWithoutPrefix = source.Substring(sliceInfo.SymbolStart);
    }

    /// <summary>
    /// 入力元文字列です。
    /// </summary>
    public string Source { get; }

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
    public bool HasPrefix { get; }

    /// <summary>
    /// 抽出されたプレフィックスです。
    /// </summary>
    public string? Prefix { get; }

    /// <summary>
    /// プレフィックスを除去したシンボル名です。
    /// </summary>
    public string SymbolNameWithoutPrefix { get; }

    /// <summary>
    /// 検査時に検出された注意事項の一覧です。
    /// </summary>
    public IReadOnlyList<SymbolInspectionWarning> Warnings { get; }

    /// <summary>
    /// 注意事項が 1 件以上存在するかを示します。
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;
}
