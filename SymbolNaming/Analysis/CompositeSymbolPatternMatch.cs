namespace SymbolNaming.Analysis;

/// <summary>
/// 複合命名パターン（ベース名 + サフィックス）の一致結果を表します。
/// </summary>
public readonly struct CompositeSymbolPatternMatch
{
    /// <summary>
    /// 一致結果を初期化します。
    /// </summary>
    public CompositeSymbolPatternMatch(string patternId, int baseStart, int baseLength, int suffixStart, int suffixLength)
    {
        if (string.IsNullOrWhiteSpace(patternId))
        {
            throw new ArgumentException("Pattern id must not be null or empty.", nameof(patternId));
        }

        if (baseStart < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseStart));
        }

        if (baseLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseLength));
        }

        if (suffixStart < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(suffixStart));
        }

        if (suffixLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(suffixLength));
        }

        PatternId = patternId;
        BaseStart = baseStart;
        BaseLength = baseLength;
        SuffixStart = suffixStart;
        SuffixLength = suffixLength;
    }

    /// <summary>
    /// 一致したパターン識別子です。
    /// </summary>
    public string PatternId { get; }

    /// <summary>
    /// ベース名の開始位置です。
    /// </summary>
    public int BaseStart { get; }

    /// <summary>
    /// ベース名の長さです。
    /// </summary>
    public int BaseLength { get; }

    /// <summary>
    /// サフィックスの開始位置です。
    /// </summary>
    public int SuffixStart { get; }

    /// <summary>
    /// サフィックスの長さです。
    /// </summary>
    public int SuffixLength { get; }
}
