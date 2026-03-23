using SymbolNaming.Dictionaries;
using SymbolNaming.Tokens;

namespace SymbolNaming.Analysis;

/// <summary>
/// 既定の Case 分類器です。
/// </summary>
public sealed class DefaultCaseClassifier : ICaseClassifier, ICaseStyleMatcher
{
    /// <summary>
    /// トークン列を分類し、分類不能時は <see cref="CaseClassificationResult.Unknown"/> を返します。
    /// </summary>
    public CaseClassificationResult Classify(TokenList tokens, CaseAnalysisOptions? options = null)
    {
        return TryClassify(tokens, out var result, options)
            ? result
            : CaseClassificationResult.Unknown;
    }

    /// <summary>
    /// 元文字列の Span を使用してトークン列を分類し、分類不能時は <see cref="CaseClassificationResult.Unknown"/> を返します。
    /// </summary>
    /// <remarks>
    /// <paramref name="tokens"/> に source がない場合でも、<paramref name="source"/> から判定できます。
    /// </remarks>
    public CaseClassificationResult Classify(ReadOnlySpan<char> source, TokenList tokens, CaseAnalysisOptions? options = null)
    {
        return TryClassify(source, tokens, out var result, options)
            ? result
            : CaseClassificationResult.Unknown;
    }

    /// <summary>
    /// 指定スタイルに適合するかどうかを判定します。
    /// </summary>
    public bool IsMatch(TokenList tokens, CaseStyle style, CaseAnalysisOptions? options = null)
    {
        return GetMatches(tokens, options).IsMatch(style);
    }

    /// <summary>
    /// 適合するスタイル候補の集合を取得します。
    /// </summary>
    public CaseStyleMatchSet GetMatches(TokenList tokens, CaseAnalysisOptions? options = null)
    {
        if (tokens is null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        if (tokens.Count == 0 || !tokens.HasSource)
        {
            return default;
        }

        options ??= new CaseAnalysisOptions();

        if (!TryGetTargetWords(tokens, options, out var targetWords, out _, out var hasUnderscoreSeparator))
        {
            return default;
        }

        return BuildMatchSet(tokens, targetWords, hasUnderscoreSeparator);
    }

    /// <summary>
    /// 元文字列の Span を使用して適合するスタイル候補の集合を取得します。
    /// </summary>
    public CaseStyleMatchSet GetMatches(ReadOnlySpan<char> source, TokenList tokens, CaseAnalysisOptions? options = null)
    {
        if (tokens is null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        if (tokens.Count == 0)
        {
            return default;
        }

        options ??= new CaseAnalysisOptions();

        if (!TryGetTargetWords(source, tokens, options, out var targetWords, out _, out var hasUnderscoreSeparator))
        {
            return default;
        }

        return BuildMatchSet(source, targetWords, hasUnderscoreSeparator);
    }

    /// <summary>
    /// トークン列の分類を試行します。
    /// </summary>
    public bool TryClassify(TokenList tokens, out CaseClassificationResult result, CaseAnalysisOptions? options = null)
    {
        if (tokens is null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        if (tokens.Count == 0)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        options ??= new CaseAnalysisOptions();

        if (!TryGetTargetWords(tokens, options, out var targetWords, out var isPrefixed, out var hasUnderscoreSeparator))
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        var style = ResolveStyle(tokens, targetWords, hasUnderscoreSeparator, options);
        if (style == CaseStyle.Unknown)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        result = new CaseClassificationResult(style, isPrefixed);
        return true;
    }

    /// <summary>
    /// 元文字列の Span を使用してトークン列の分類を試行します。
    /// </summary>
    public bool TryClassify(ReadOnlySpan<char> source, TokenList tokens, out CaseClassificationResult result, CaseAnalysisOptions? options = null)
    {
        if (tokens is null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        if (tokens.Count == 0)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        options ??= new CaseAnalysisOptions();

        if (!TryGetTargetWords(source, tokens, options, out var targetWords, out var isPrefixed, out var hasUnderscoreSeparator))
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        var style = ResolveStyle(source, targetWords, hasUnderscoreSeparator, options);
        if (style == CaseStyle.Unknown)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        result = new CaseClassificationResult(style, isPrefixed);
        return true;
    }

    private static bool TryGetTargetWords(TokenList tokens, CaseAnalysisOptions options, out List<Token> targetWords, out bool isPrefixed, out bool hasUnderscoreSeparator)
    {
        targetWords = new List<Token>();
        isPrefixed = false;
        hasUnderscoreSeparator = false;

        if (!tokens.HasSource)
        {
            return false;
        }

        var words = CollectWordLikeTokens(tokens);
        if (words.Count == 0)
        {
            return false;
        }

        isPrefixed = words.Count >= 2 && IsPrefixToken(words[0], tokens, options);
        targetWords = isPrefixed ? SliceTail(words) : words;
        hasUnderscoreSeparator = HasMeaningfulUnderscoreSeparator(tokens, options);
        return targetWords.Count > 0;
    }

    private static bool TryGetTargetWords(ReadOnlySpan<char> source, TokenList tokens, CaseAnalysisOptions options, out List<Token> targetWords, out bool isPrefixed, out bool hasUnderscoreSeparator)
    {
        targetWords = new List<Token>();
        isPrefixed = false;
        hasUnderscoreSeparator = false;

        var words = CollectWordLikeTokens(tokens);
        if (words.Count == 0)
        {
            return false;
        }

        isPrefixed = words.Count >= 2 && IsPrefixToken(words[0], source, options);
        targetWords = isPrefixed ? SliceTail(words) : words;
        hasUnderscoreSeparator = HasMeaningfulUnderscoreSeparator(tokens, source, options);
        return targetWords.Count > 0;
    }

    private static List<Token> CollectWordLikeTokens(TokenList tokens)
    {
        var words = new List<Token>(tokens.Count);
        foreach (var token in tokens)
        {
            if (token.Category == TokenCategory.Word || token.Category == TokenCategory.Dictionary || token.Category == TokenCategory.Prefix)
            {
                words.Add(token);
            }
        }

        return words;
    }

    private static List<Token> SliceTail(List<Token> words)
    {
        var targetWords = new List<Token>(words.Count - 1);
        for (var i = 1; i < words.Count; i++)
        {
            targetWords.Add(words[i]);
        }

        return targetWords;
    }

    private static CaseStyle ResolveStyle(TokenList tokens, List<Token> targetWords, bool hasUnderscoreSeparator, CaseAnalysisOptions options)
    {
        var matches = BuildMatchSet(tokens, targetWords, hasUnderscoreSeparator);
        return ResolveStyleFromMatches(matches, targetWords.Count, hasUnderscoreSeparator, tokens.GetSpan(targetWords[0]), options);
    }

    private static CaseStyle ResolveStyle(ReadOnlySpan<char> source, List<Token> targetWords, bool hasUnderscoreSeparator, CaseAnalysisOptions options)
    {
        var matches = BuildMatchSet(source, targetWords, hasUnderscoreSeparator);
        return ResolveStyleFromMatches(matches, targetWords.Count, hasUnderscoreSeparator, source.Slice(targetWords[0].Start, targetWords[0].Length), options);
    }

    private static CaseStyleMatchSet BuildMatchSet(TokenList tokens, List<Token> targetWords, bool hasUnderscoreSeparator)
    {
        if (targetWords.Count == 0)
        {
            return default;
        }

        if (hasUnderscoreSeparator)
        {
            return new CaseStyleMatchSet(
                pascalCase: false,
                camelCase: false,
                upperSnakeCase: AllWordsPascal(targetWords, tokens),
                lowerSnakeCase: AllWordsLower(targetWords, tokens),
                screamingSnakeCase: AllWordsUpper(targetWords, tokens));
        }

        var firstWordSpan = tokens.GetSpan(targetWords[0]);
        if (firstWordSpan.Length == 0)
        {
            return default;
        }

        var startsUpper = char.IsUpper(firstWordSpan[0]);
        var startsLower = char.IsLower(firstWordSpan[0]);

        if (targetWords.Count > 1)
        {
            return new CaseStyleMatchSet(
                pascalCase: startsUpper,
                camelCase: startsLower,
                upperSnakeCase: false,
                lowerSnakeCase: false,
                screamingSnakeCase: false);
        }

        return BuildSingleWordNoSeparatorMatchSet(firstWordSpan, startsLower);
    }

    private static CaseStyleMatchSet BuildMatchSet(ReadOnlySpan<char> source, List<Token> targetWords, bool hasUnderscoreSeparator)
    {
        if (targetWords.Count == 0)
        {
            return default;
        }

        if (hasUnderscoreSeparator)
        {
            return new CaseStyleMatchSet(
                pascalCase: false,
                camelCase: false,
                upperSnakeCase: AllWordsPascal(targetWords, source),
                lowerSnakeCase: AllWordsLower(targetWords, source),
                screamingSnakeCase: AllWordsUpper(targetWords, source));
        }

        var firstWordSpan = source.Slice(targetWords[0].Start, targetWords[0].Length);
        if (firstWordSpan.Length == 0)
        {
            return default;
        }

        var startsUpper = char.IsUpper(firstWordSpan[0]);
        var startsLower = char.IsLower(firstWordSpan[0]);

        if (targetWords.Count > 1)
        {
            return new CaseStyleMatchSet(
                pascalCase: startsUpper,
                camelCase: startsLower,
                upperSnakeCase: false,
                lowerSnakeCase: false,
                screamingSnakeCase: false);
        }

        return BuildSingleWordNoSeparatorMatchSet(firstWordSpan, startsLower);
    }

    private static CaseStyleMatchSet BuildSingleWordNoSeparatorMatchSet(ReadOnlySpan<char> text, bool startsLower)
    {
        return new CaseStyleMatchSet(
            pascalCase: IsPascalWord(text),
            camelCase: startsLower,
            upperSnakeCase: IsPascalWord(text),
            lowerSnakeCase: AllLowerWord(text),
            screamingSnakeCase: ContainsLetterWithCase(text, isUpper: true));
    }

    private static CaseStyle ResolveStyleFromMatches(CaseStyleMatchSet matches, int targetWordCount, bool hasUnderscoreSeparator, ReadOnlySpan<char> firstWordSpan, CaseAnalysisOptions options)
    {
        if (!matches.HasAny)
        {
            return CaseStyle.Unknown;
        }

        if (hasUnderscoreSeparator || targetWordCount > 1 || matches.Count == 1)
        {
            return SelectSingleMatch(matches);
        }

        switch (options.AmbiguousSingleTokenPolicy)
        {
            case AmbiguousSingleTokenPolicy.PreferSnakeCase:
                return ResolveBySnakePreferred(matches);
            case AmbiguousSingleTokenPolicy.ReturnUnknown:
                return CaseStyle.Unknown;
            case AmbiguousSingleTokenPolicy.UseCustomResolver:
                return ResolveByCustomResolver(matches, options);
            default:
                return ResolveByPascalCamelCompatibility(matches, firstWordSpan);
        }
    }

    private static CaseStyle SelectSingleMatch(CaseStyleMatchSet matches)
    {
        if (matches.PascalCase)
        {
            return CaseStyle.PascalCase;
        }

        if (matches.CamelCase)
        {
            return CaseStyle.CamelCase;
        }

        if (matches.UpperSnakeCase)
        {
            return CaseStyle.UpperSnakeCase;
        }

        if (matches.LowerSnakeCase)
        {
            return CaseStyle.LowerSnakeCase;
        }

        if (matches.ScreamingSnakeCase)
        {
            return CaseStyle.ScreamingSnakeCase;
        }

        return CaseStyle.Unknown;
    }

    private static CaseStyle ResolveByPascalCamelCompatibility(CaseStyleMatchSet matches, ReadOnlySpan<char> firstWordSpan)
    {
        if (firstWordSpan.Length > 0 && char.IsUpper(firstWordSpan[0]) && matches.PascalCase)
        {
            return CaseStyle.PascalCase;
        }

        if (firstWordSpan.Length > 0 && char.IsLower(firstWordSpan[0]) && matches.CamelCase)
        {
            return CaseStyle.CamelCase;
        }

        if (matches.PascalCase)
        {
            return CaseStyle.PascalCase;
        }

        if (matches.CamelCase)
        {
            return CaseStyle.CamelCase;
        }

        if (matches.ScreamingSnakeCase)
        {
            return CaseStyle.ScreamingSnakeCase;
        }

        if (matches.UpperSnakeCase)
        {
            return CaseStyle.UpperSnakeCase;
        }

        if (matches.LowerSnakeCase)
        {
            return CaseStyle.LowerSnakeCase;
        }

        return CaseStyle.Unknown;
    }

    private static CaseStyle ResolveBySnakePreferred(CaseStyleMatchSet matches)
    {
        if (matches.ScreamingSnakeCase)
        {
            return CaseStyle.ScreamingSnakeCase;
        }

        if (matches.UpperSnakeCase)
        {
            return CaseStyle.UpperSnakeCase;
        }

        if (matches.LowerSnakeCase)
        {
            return CaseStyle.LowerSnakeCase;
        }

        if (matches.PascalCase)
        {
            return CaseStyle.PascalCase;
        }

        if (matches.CamelCase)
        {
            return CaseStyle.CamelCase;
        }

        return CaseStyle.Unknown;
    }

    private static CaseStyle ResolveByCustomResolver(CaseStyleMatchSet matches, CaseAnalysisOptions options)
    {
        var resolver = options.AmbiguousSingleTokenResolver;
        if (resolver is null)
        {
            return CaseStyle.Unknown;
        }

        var resolved = resolver(matches);
        if (!resolved.HasValue || !matches.IsMatch(resolved.Value))
        {
            return CaseStyle.Unknown;
        }

        return resolved.Value;
    }

    private static bool AllLowerWord(ReadOnlySpan<char> text)
    {
        return ContainsLetterWithCase(text, isUpper: false);
    }

    private static bool AllWordsLower(IEnumerable<Token> words, TokenList tokens)
    {
        foreach (var word in words)
        {
            if (!ContainsLetterWithCase(tokens.GetSpan(word), isUpper: false))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllWordsLower(IEnumerable<Token> words, ReadOnlySpan<char> source)
    {
        foreach (var word in words)
        {
            if (!ContainsLetterWithCase(source.Slice(word.Start, word.Length), isUpper: false))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllWordsUpper(IEnumerable<Token> words, TokenList tokens)
    {
        foreach (var word in words)
        {
            if (!ContainsLetterWithCase(tokens.GetSpan(word), isUpper: true))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllWordsUpper(IEnumerable<Token> words, ReadOnlySpan<char> source)
    {
        foreach (var word in words)
        {
            if (!ContainsLetterWithCase(source.Slice(word.Start, word.Length), isUpper: true))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllWordsPascal(IEnumerable<Token> words, TokenList tokens)
    {
        foreach (var word in words)
        {
            if (!IsPascalWord(tokens.GetSpan(word)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllWordsPascal(IEnumerable<Token> words, ReadOnlySpan<char> source)
    {
        foreach (var word in words)
        {
            if (!IsPascalWord(source.Slice(word.Start, word.Length)))
            {
                return false;
            }
        }

        return true;
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

    private static bool ContainsLetterWithCase(ReadOnlySpan<char> text, bool isUpper)
    {
        var hasLetter = false;
        foreach (var c in text)
        {
            if (!char.IsLetter(c))
            {
                continue;
            }

            hasLetter = true;
            if (isUpper && !char.IsUpper(c))
            {
                return false;
            }

            if (!isUpper && !char.IsLower(c))
            {
                return false;
            }
        }

        return hasLetter;
    }

    private static bool IsPrefixToken(Token token, TokenList tokens, CaseAnalysisOptions options)
    {
        if (token.Category == TokenCategory.Prefix)
        {
            return true;
        }

        if (token.Category == TokenCategory.Dictionary)
        {
            return false;
        }

        var tokenSpan = tokens.GetSpan(token);
        if (tokenSpan.Length == 0)
        {
            return false;
        }

        if (options.ProtectedWordProvider.IsProtected(tokenSpan))
        {
            return false;
        }

        if (options.PrefixProvider.IsPrefix(tokenSpan))
        {
            return true;
        }

        return false;
    }

    private static bool IsPrefixToken(Token token, ReadOnlySpan<char> source, CaseAnalysisOptions options)
    {
        if (token.Category == TokenCategory.Prefix)
        {
            return true;
        }

        if (token.Category == TokenCategory.Dictionary)
        {
            return false;
        }

        var tokenSpan = source.Slice(token.Start, token.Length);
        if (tokenSpan.Length == 0)
        {
            return false;
        }

        if (options.ProtectedWordProvider.IsProtected(tokenSpan))
        {
            return false;
        }

        if (options.PrefixProvider.IsPrefix(tokenSpan))
        {
            return true;
        }

        return false;
    }

    private static bool IsUnderscore(Token token, TokenList tokens)
    {
        var span = tokens.GetSpan(token);
        return span.Length == 1 && span[0] == '_';
    }

    private static bool IsUnderscore(Token token, ReadOnlySpan<char> source)
    {
        var span = source.Slice(token.Start, token.Length);
        return span.Length == 1 && span[0] == '_';
    }

    private static bool HasMeaningfulUnderscoreSeparator(TokenList tokens, CaseAnalysisOptions options)
    {
        for (var i = 0; i < tokens.Count; ++i)
        {
            var token = tokens[i];
            if (token.Category != TokenCategory.Separator || !IsUnderscore(token, tokens))
            {
                continue;
            }

            if (IsPrefixConnectorUnderscore(tokens, i, options))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool HasMeaningfulUnderscoreSeparator(TokenList tokens, ReadOnlySpan<char> source, CaseAnalysisOptions options)
    {
        for (var i = 0; i < tokens.Count; ++i)
        {
            var token = tokens[i];
            if (token.Category != TokenCategory.Separator || !IsUnderscore(token, source))
            {
                continue;
            }

            if (IsPrefixConnectorUnderscore(tokens, source, i, options))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool IsPrefixConnectorUnderscore(TokenList tokens, int separatorIndex, CaseAnalysisOptions options)
    {
        var prevIndex = separatorIndex - 1;
        if (prevIndex < 0)
        {
            return false;
        }

        var prev = tokens[prevIndex];
        if (prev.Category != TokenCategory.Word && prev.Category != TokenCategory.Dictionary && prev.Category != TokenCategory.Prefix)
        {
            return false;
        }

        for (var i = 0; i < prevIndex; ++i)
        {
            var category = tokens[i].Category;
            if (category == TokenCategory.Word || category == TokenCategory.Dictionary || category == TokenCategory.Prefix)
            {
                return false;
            }
        }

        if (!IsPrefixToken(prev, tokens, options))
        {
            return false;
        }

        var nextIndex = separatorIndex + 1;
        if (nextIndex >= tokens.Count)
        {
            return false;
        }

        var next = tokens[nextIndex];
        return next.Category == TokenCategory.Word || next.Category == TokenCategory.Dictionary || next.Category == TokenCategory.Prefix;
    }

    private static bool IsPrefixConnectorUnderscore(TokenList tokens, ReadOnlySpan<char> source, int separatorIndex, CaseAnalysisOptions options)
    {
        var prevIndex = separatorIndex - 1;
        if (prevIndex < 0)
        {
            return false;
        }

        var prev = tokens[prevIndex];
        if (prev.Category != TokenCategory.Word && prev.Category != TokenCategory.Dictionary && prev.Category != TokenCategory.Prefix)
        {
            return false;
        }

        for (var i = 0; i < prevIndex; ++i)
        {
            var category = tokens[i].Category;
            if (category == TokenCategory.Word || category == TokenCategory.Dictionary || category == TokenCategory.Prefix)
            {
                return false;
            }
        }

        if (!IsPrefixToken(prev, source, options))
        {
            return false;
        }

        var nextIndex = separatorIndex + 1;
        if (nextIndex >= tokens.Count)
        {
            return false;
        }

        var next = tokens[nextIndex];
        return next.Category == TokenCategory.Word || next.Category == TokenCategory.Dictionary || next.Category == TokenCategory.Prefix;
    }
}
