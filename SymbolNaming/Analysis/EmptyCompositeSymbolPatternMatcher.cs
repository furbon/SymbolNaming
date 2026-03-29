using SymbolNaming.Tokens;

namespace SymbolNaming.Analysis;

/// <summary>
/// 常に不一致を返す複合パターン判定器です。
/// </summary>
public sealed class EmptyCompositeSymbolPatternMatcher : ICompositeSymbolPatternMatcher
{
    /// <summary>
    /// 共有インスタンスです。
    /// </summary>
    public static EmptyCompositeSymbolPatternMatcher Instance { get; } = new();

    private EmptyCompositeSymbolPatternMatcher()
    {
    }

    /// <inheritdoc />
    public bool TryMatch(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification, out CompositeSymbolPatternMatch match)
    {
        match = default;
        return false;
    }
}
