using SymbolNaming.Tokens;

namespace SymbolNaming.Analysis;

/// <summary>
/// シンボル名の複合命名パターン一致判定を提供するインターフェイスです。
/// </summary>
public interface ICompositeSymbolPatternMatcher
{
    /// <summary>
    /// 入力から複合命名パターンの一致判定を試行します。
    /// </summary>
    bool TryMatch(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification, out CompositeSymbolPatternMatch match);
}
