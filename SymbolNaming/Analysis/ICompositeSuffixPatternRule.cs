namespace SymbolNaming.Analysis;

/// <summary>
/// ベース名・サフィックスの組み合わせルールを表します。
/// </summary>
public interface ICompositeSuffixPatternRule
{
    /// <summary>
    /// ルール識別子です。
    /// </summary>
    string PatternId { get; }

    /// <summary>
    /// 指定のベース名・サフィックスが一致するかを判定します。
    /// </summary>
    bool IsMatch(ReadOnlySpan<char> baseName, ReadOnlySpan<char> suffix);
}
