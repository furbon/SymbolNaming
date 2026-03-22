using SymbolNaming.Analysis;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

/// <summary>
/// プレフィックス抽出結果のスライス情報を保持します。
/// </summary>
internal readonly struct SymbolInspectionSliceInfo
{
    /// <summary>
    /// 新しいスライス情報を初期化します。
    /// </summary>
    public SymbolInspectionSliceInfo(int prefixStart, int prefixLength, int symbolStart)
    {
        PrefixStart = prefixStart;
        PrefixLength = prefixLength;
        SymbolStart = symbolStart;
    }

    /// <summary>
    /// プレフィックス開始位置です。
    /// </summary>
    public int PrefixStart { get; }

    /// <summary>
    /// プレフィックス長です。
    /// </summary>
    public int PrefixLength { get; }

    /// <summary>
    /// プレフィックス除去後シンボルの開始位置です。
    /// </summary>
    public int SymbolStart { get; }
}

/// <summary>
/// <see cref="SymbolInspectionSliceInfo"/> を計算するファクトリーです。
/// </summary>
internal static class SymbolInspectionSliceInfoFactory
{
    /// <summary>
    /// 入力情報からスライス情報を生成します。
    /// </summary>
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
