using System.Text.RegularExpressions;

namespace SymbolNaming.Analysis;

/// <summary>
/// 正規表現でベース名・サフィックス一致を判定するルールです。
/// </summary>
public sealed class RegexCompositeSuffixPatternRule : ICompositeSuffixPatternRule
{
    private readonly Regex _baseRegex;
    private readonly Regex _suffixRegex;

    /// <summary>
    /// ルールを初期化します。
    /// </summary>
    public RegexCompositeSuffixPatternRule(string patternId, string suffixPattern, string basePattern = "^.+$", RegexOptions regexOptions = RegexOptions.CultureInvariant)
    {
        if (string.IsNullOrWhiteSpace(patternId))
        {
            throw new ArgumentException("Pattern id must not be null or empty.", nameof(patternId));
        }

        if (string.IsNullOrWhiteSpace(suffixPattern))
        {
            throw new ArgumentException("Suffix pattern must not be null or empty.", nameof(suffixPattern));
        }

        if (string.IsNullOrWhiteSpace(basePattern))
        {
            throw new ArgumentException("Base pattern must not be null or empty.", nameof(basePattern));
        }

        PatternId = patternId;
        _baseRegex = new Regex(basePattern, regexOptions);
        _suffixRegex = new Regex(suffixPattern, regexOptions);
    }

    /// <inheritdoc />
    public string PatternId { get; }

    /// <inheritdoc />
    public bool IsMatch(ReadOnlySpan<char> baseName, ReadOnlySpan<char> suffix)
    {
        if (baseName.IsEmpty || suffix.IsEmpty)
        {
            return false;
        }

        return _baseRegex.IsMatch(baseName.ToString()) && _suffixRegex.IsMatch(suffix.ToString());
    }
}
