using SymbolNaming.Tokens;

namespace SymbolNaming.Analysis;

/// <summary>
/// ベース名 + アンダースコア + サフィックス形式を対象に複合パターンを判定します。
/// </summary>
public sealed class CompositeSuffixPatternMatcher : ICompositeSymbolPatternMatcher
{
    private readonly ICompositeSuffixPatternRule[] _rules;

    /// <summary>
    /// ルール一覧を指定して初期化します。
    /// </summary>
    public CompositeSuffixPatternMatcher(params ICompositeSuffixPatternRule[] rules)
        : this((IEnumerable<ICompositeSuffixPatternRule>)rules)
    {
    }

    /// <summary>
    /// ルール一覧を指定して初期化します。
    /// </summary>
    public CompositeSuffixPatternMatcher(IEnumerable<ICompositeSuffixPatternRule> rules)
    {
        if (rules is null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        _rules = rules.ToArray();
        if (_rules.Length == 0)
        {
            throw new ArgumentException("At least one rule is required.", nameof(rules));
        }

        var patternIds = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < _rules.Length; i++)
        {
            if (_rules[i] is null)
            {
                throw new ArgumentException("Rules must not contain null.", nameof(rules));
            }

            var patternId = _rules[i].PatternId;
            if (string.IsNullOrWhiteSpace(patternId))
            {
                throw new ArgumentException("Pattern id must not be null or empty.", nameof(rules));
            }

            if (!patternIds.Add(patternId))
            {
                throw new ArgumentException($"Duplicate pattern id is not allowed: '{patternId}'.", nameof(rules));
            }
        }
    }

    /// <inheritdoc />
    public bool TryMatch(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification, out CompositeSymbolPatternMatch match)
    {
        if (tokens is null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        if (!TrySplitBaseAndSuffix(source, tokens, out var baseStart, out var baseLength, out var suffixStart, out var suffixLength))
        {
            match = default;
            return false;
        }

        var baseSpan = source.Slice(baseStart, baseLength);
        var suffixSpan = source.Slice(suffixStart, suffixLength);
        string? baseText = null;
        string? suffixText = null;

        for (var i = 0; i < _rules.Length; i++)
        {
            var rule = _rules[i];
            if (!IsRuleMatch(rule, baseSpan, suffixSpan, ref baseText, ref suffixText))
            {
                continue;
            }

            match = new CompositeSymbolPatternMatch(rule.PatternId, baseStart, baseLength, suffixStart, suffixLength);
            return true;
        }

        match = default;
        return false;
    }

    private static bool IsRuleMatch(
        ICompositeSuffixPatternRule rule,
        ReadOnlySpan<char> baseSpan,
        ReadOnlySpan<char> suffixSpan,
        ref string? baseText,
        ref string? suffixText)
    {
        if (rule is ICompositeSuffixPatternRuleRuntime optimizedRule)
        {
            baseText ??= baseSpan.ToString();
            suffixText ??= suffixSpan.ToString();
            return optimizedRule.IsMatch(baseText, suffixText);
        }

        return rule.IsMatch(baseSpan, suffixSpan);
    }

    private static bool TrySplitBaseAndSuffix(ReadOnlySpan<char> source, TokenList tokens, out int baseStart, out int baseLength, out int suffixStart, out int suffixLength)
    {
        baseStart = 0;
        baseLength = 0;
        suffixStart = 0;
        suffixLength = 0;

        var symbolStart = GetSymbolStart(tokens, source);
        if ((uint)symbolStart >= (uint)source.Length)
        {
            return false;
        }

        var symbol = source.Slice(symbolStart);
        var separatorOffset = symbol.IndexOf('_');
        if (separatorOffset <= 0)
        {
            return false;
        }

        var restOffset = separatorOffset + 1;
        if (restOffset >= symbol.Length)
        {
            return false;
        }

        baseStart = symbolStart;
        baseLength = separatorOffset;
        suffixStart = symbolStart + restOffset;
        suffixLength = symbol.Length - restOffset;

        return suffixLength > 0;
    }

    private static int GetSymbolStart(TokenList tokens, ReadOnlySpan<char> source)
    {
        var firstWordLikeIndex = -1;
        for (var i = 0; i < tokens.Count; i++)
        {
            var category = tokens[i].Category;
            if (category == TokenCategory.Word || category == TokenCategory.Dictionary || category == TokenCategory.Prefix)
            {
                firstWordLikeIndex = i;
                break;
            }
        }

        if (firstWordLikeIndex < 0)
        {
            return 0;
        }

        if (tokens[firstWordLikeIndex].Category != TokenCategory.Prefix)
        {
            return 0;
        }

        var prefixToken = tokens[firstWordLikeIndex];
        var symbolStart = prefixToken.Start + prefixToken.Length;

        var connectorIndex = firstWordLikeIndex + 1;
        if (connectorIndex < tokens.Count && IsPrefixConnectorUnderscore(tokens[connectorIndex], source))
        {
            var connector = tokens[connectorIndex];
            symbolStart = connector.Start + connector.Length;
        }

        return symbolStart;
    }

    private static bool IsPrefixConnectorUnderscore(Token token, ReadOnlySpan<char> source)
    {
        return token.Category == TokenCategory.Separator
            && token.Length == 1
            && source[token.Start] == '_';
    }
}
