using SymbolNaming.Analysis;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

internal sealed class SymbolInspectionWarningAnalyzer : IInspectionRule
{
    public bool TryCreateWarning(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification, out SymbolInspectionWarning warning)
    {
        if (tokens.Count == 0)
        {
            warning = default;
            return false;
        }

        if (!TryGetFirstTwoTargetWordLikeTokenIndices(source, tokens, classification, out var firstTargetIndex, out var secondTargetIndex))
        {
            warning = default;
            return false;
        }

        var first = tokens[firstTargetIndex];
        var firstSpan = source.Slice(first.Start, first.Length);
        if (firstSpan.Length != 1 || !char.IsUpper(firstSpan[0]))
        {
            warning = default;
            return false;
        }

        if (firstSpan[0] == 'I')
        {
            warning = default;
            return false;
        }

        var second = tokens[secondTargetIndex];
        var secondSpan = source.Slice(second.Start, second.Length);
        if (!IsPascalWord(secondSpan))
        {
            warning = default;
            return false;
        }

        warning = new SymbolInspectionWarning(SymbolInspectionWarningKind.SuspiciousLeadingSingleUpperToken, first.Start, first.Length);
        return true;
    }

    private static bool TryGetFirstTwoTargetWordLikeTokenIndices(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification, out int firstTargetIndex, out int secondTargetIndex)
    {
        firstTargetIndex = -1;
        secondTargetIndex = -1;

        var seenWordLike = 0;
        var skipWordLikeCount = classification.Prefixed ? 1 : 0;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!IsWordLike(token.Category))
            {
                continue;
            }

            if (seenWordLike < skipWordLikeCount)
            {
                seenWordLike++;
                continue;
            }

            if (firstTargetIndex < 0)
            {
                firstTargetIndex = i;
                continue;
            }

            secondTargetIndex = i;
            break;
        }

        if (firstTargetIndex < 0 || secondTargetIndex < 0)
        {
            return false;
        }

        if (classification.Prefixed && firstTargetIndex > 0)
        {
            var connectorIndex = firstTargetIndex - 1;
            if (tokens[connectorIndex].Category == TokenCategory.Separator && IsUnderscore(tokens[connectorIndex], source))
            {
                var prefixIndex = connectorIndex - 1;
                if (prefixIndex >= 0 && IsWordLike(tokens[prefixIndex].Category))
                {
                    return true;
                }
            }
        }

        return true;
    }

    private static bool IsUnderscore(Token token, ReadOnlySpan<char> source)
    {
        return token.Length == 1 && source[token.Start] == '_';
    }

    private static bool IsWordLike(TokenCategory category)
    {
        return category == TokenCategory.Word || category == TokenCategory.Dictionary || category == TokenCategory.Prefix;
    }

    private static bool IsPascalWord(ReadOnlySpan<char> text)
    {
        var firstLetterFound = false;
        var hasLetter = false;

        foreach (var c in text)
        {
            if (!char.IsLetter(c))
            {
                continue;
            }

            hasLetter = true;
            if (!firstLetterFound)
            {
                if (!char.IsUpper(c))
                {
                    return false;
                }

                firstLetterFound = true;
                continue;
            }

            if (!char.IsLower(c))
            {
                return false;
            }
        }

        return hasLetter;
    }
}
