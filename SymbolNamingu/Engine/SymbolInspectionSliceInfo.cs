using SymbolNaming.Analysis;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

internal readonly struct SymbolInspectionSliceInfo
{
    public SymbolInspectionSliceInfo(int prefixStart, int prefixLength, int symbolStart)
    {
        PrefixStart = prefixStart;
        PrefixLength = prefixLength;
        SymbolStart = symbolStart;
    }

    public int PrefixStart { get; }

    public int PrefixLength { get; }

    public int SymbolStart { get; }
}

internal static class SymbolInspectionSliceInfoFactory
{
    public static SymbolInspectionSliceInfo Create(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification)
    {
        if (!classification.Prefixed)
        {
            return new SymbolInspectionSliceInfo(0, 0, 0);
        }

        var firstWordIndex = FindFirstWordLikeTokenIndex(tokens);
        if (firstWordIndex < 0)
        {
            return new SymbolInspectionSliceInfo(0, 0, 0);
        }

        var prefixToken = tokens[firstWordIndex];
        var symbolStart = prefixToken.Start + prefixToken.Length;

        var connectorIndex = firstWordIndex + 1;
        if (connectorIndex < tokens.Count && IsPrefixConnectorUnderscore(tokens, source, connectorIndex))
        {
            var connector = tokens[connectorIndex];
            symbolStart = connector.Start + connector.Length;
        }

        return new SymbolInspectionSliceInfo(prefixToken.Start, prefixToken.Length, symbolStart);
    }

    private static int FindFirstWordLikeTokenIndex(TokenList tokens)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            var category = tokens[i].Category;
            if (category == TokenCategory.Word || category == TokenCategory.Dictionary || category == TokenCategory.Prefix)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsPrefixConnectorUnderscore(TokenList tokens, ReadOnlySpan<char> source, int separatorIndex)
    {
        var separator = tokens[separatorIndex];
        if (separator.Category != TokenCategory.Separator || separator.Length != 1 || source[separator.Start] != '_')
        {
            return false;
        }

        var nextIndex = separatorIndex + 1;
        if (nextIndex >= tokens.Count)
        {
            return false;
        }

        var nextCategory = tokens[nextIndex].Category;
        return nextCategory == TokenCategory.Word || nextCategory == TokenCategory.Dictionary || nextCategory == TokenCategory.Prefix;
    }
}
