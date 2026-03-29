using SymbolNaming.Analysis;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

/// <summary>
/// 文字列入力に対する解析用正規化結果を保持します。
/// </summary>
public sealed class SymbolNormalizationResult
{
    internal SymbolNormalizationResult(string source, TokenList tokens, CaseClassificationResult classification)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        Classification = classification;

        var sliceInfo = SymbolInspectionSliceInfoFactory.Create(source.AsSpan(), tokens, classification);
        HasPrefix = sliceInfo.PrefixLength > 0;
        Prefix = HasPrefix ? source.Substring(sliceInfo.PrefixStart, sliceInfo.PrefixLength) : null;
        SymbolNameWithoutPrefix = source.Substring(sliceInfo.SymbolStart);
        NormalizedStart = sliceInfo.NormalizedStart;
        NormalizedLength = sliceInfo.NormalizedLength;
        NormalizedSymbol = NormalizedLength > 0
            ? source.Substring(NormalizedStart, NormalizedLength)
            : string.Empty;
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
    /// 先頭/末尾の装飾情報です。
    /// </summary>
    public SymbolDecorationInfo Decoration => Classification.Decoration;

    /// <summary>
    /// 先頭連続アンダースコア数です。
    /// </summary>
    public int LeadingUnderscoreCount => Decoration.LeadingUnderscoreCount;

    /// <summary>
    /// 末尾連続アンダースコア数です。
    /// </summary>
    public int TrailingUnderscoreCount => Decoration.TrailingUnderscoreCount;

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
    /// 解析用正規化シンボルの開始位置です。
    /// </summary>
    public int NormalizedStart { get; }

    /// <summary>
    /// 解析用正規化シンボルの長さです。
    /// </summary>
    public int NormalizedLength { get; }

    /// <summary>
    /// 解析用にプレフィックスと先頭/末尾装飾を除去したシンボルです。
    /// </summary>
    public string NormalizedSymbol { get; }
}

/// <summary>
/// Span 入力に対する解析用正規化結果を保持します。
/// </summary>
public readonly ref struct SymbolNormalizationSpan
{
    private readonly ReadOnlySpan<char> _source;
    private readonly SymbolInspectionSliceInfo _sliceInfo;

    internal SymbolNormalizationSpan(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification)
    {
        _source = source;
        Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        Classification = classification;
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
    /// 先頭/末尾の装飾情報です。
    /// </summary>
    public SymbolDecorationInfo Decoration => Classification.Decoration;

    /// <summary>
    /// 先頭連続アンダースコア数です。
    /// </summary>
    public int LeadingUnderscoreCount => Decoration.LeadingUnderscoreCount;

    /// <summary>
    /// 末尾連続アンダースコア数です。
    /// </summary>
    public int TrailingUnderscoreCount => Decoration.TrailingUnderscoreCount;

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
    /// 解析用正規化シンボルの開始位置です。
    /// </summary>
    public int NormalizedStart => _sliceInfo.NormalizedStart;

    /// <summary>
    /// 解析用正規化シンボルの長さです。
    /// </summary>
    public int NormalizedLength => _sliceInfo.NormalizedLength;

    /// <summary>
    /// 解析用にプレフィックスと先頭/末尾装飾を除去したシンボル Span です。
    /// </summary>
    public ReadOnlySpan<char> NormalizedSymbol =>
        NormalizedLength > 0
            ? _source.Slice(NormalizedStart, NormalizedLength)
            : ReadOnlySpan<char>.Empty;
}
