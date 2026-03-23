using SymbolNaming.Dictionaries;
using SymbolNaming.Tokens;

namespace SymbolNaming.Analysis;

/// <summary>
/// 既定の Case 分類器です。
/// </summary>
public sealed class DefaultCaseClassifier : ICaseClassifier, ICaseStyleMatcher
{
    private static readonly CaseAnalysisOptions DefaultOptions = new();

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

        options ??= DefaultOptions;

        if (!TryGetTargetWords(tokens, options, out var targetWords))
        {
            return default;
        }

        return BuildMatchSet(tokens, targetWords);
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

        options ??= DefaultOptions;

        if (!TryGetTargetWords(source, tokens, options, out var targetWords))
        {
            return default;
        }

        return BuildMatchSet(source, tokens, targetWords);
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

        options ??= DefaultOptions;

        if (!TryGetTargetWords(tokens, options, out var targetWords))
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        var style = ResolveStyle(tokens, targetWords, options);
        if (style == CaseStyle.Unknown)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        result = new CaseClassificationResult(style, targetWords.IsPrefixed);
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

        options ??= DefaultOptions;

        if (!TryGetTargetWords(source, tokens, options, out var targetWords))
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        var style = ResolveStyle(source, tokens, targetWords, options);
        if (style == CaseStyle.Unknown)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        result = new CaseClassificationResult(style, targetWords.IsPrefixed);
        return true;
    }

    private static bool TryGetTargetWords(TokenList tokens, CaseAnalysisOptions options, out TargetWordInfo targetWords)
    {
        targetWords = default;

        if (!tokens.HasSource)
        {
            return false;
        }

        if (!TryGetWordLikeInfo(tokens, out var wordLikeCount, out var firstWordLikeIndex, out var secondWordLikeIndex))
        {
            return false;
        }

        var isPrefixed = wordLikeCount >= 2 && IsPrefixToken(tokens[firstWordLikeIndex], tokens, options);
        var firstTargetIndex = isPrefixed ? secondWordLikeIndex : firstWordLikeIndex;
        var targetWordCount = isPrefixed ? wordLikeCount - 1 : wordLikeCount;

        if (firstTargetIndex < 0 || targetWordCount <= 0)
        {
            return false;
        }

        targetWords = new TargetWordInfo(
            firstTargetIndex,
            targetWordCount,
            isPrefixed,
            HasMeaningfulUnderscoreSeparator(tokens, options));

        return true;
    }

    private static bool TryGetTargetWords(ReadOnlySpan<char> source, TokenList tokens, CaseAnalysisOptions options, out TargetWordInfo targetWords)
    {
        targetWords = default;

        if (!TryGetWordLikeInfo(tokens, out var wordLikeCount, out var firstWordLikeIndex, out var secondWordLikeIndex))
        {
            return false;
        }

        var isPrefixed = wordLikeCount >= 2 && IsPrefixToken(tokens[firstWordLikeIndex], source, options);
        var firstTargetIndex = isPrefixed ? secondWordLikeIndex : firstWordLikeIndex;
        var targetWordCount = isPrefixed ? wordLikeCount - 1 : wordLikeCount;

        if (firstTargetIndex < 0 || targetWordCount <= 0)
        {
            return false;
        }

        targetWords = new TargetWordInfo(
            firstTargetIndex,
            targetWordCount,
            isPrefixed,
            HasMeaningfulUnderscoreSeparator(tokens, source, options));

        return true;
    }

    private static bool TryGetWordLikeInfo(TokenList tokens, out int wordLikeCount, out int firstWordLikeIndex, out int secondWordLikeIndex)
    {
        wordLikeCount = 0;
        firstWordLikeIndex = -1;
        secondWordLikeIndex = -1;

        for (var i = 0; i < tokens.Count; i++)
        {
            if (!IsWordLike(tokens[i].Category))
            {
                continue;
            }

            wordLikeCount++;
            if (firstWordLikeIndex < 0)
            {
                firstWordLikeIndex = i;
            }
            else if (secondWordLikeIndex < 0)
            {
                secondWordLikeIndex = i;
            }
        }

        return firstWordLikeIndex >= 0;
    }

    private static bool IsWordLike(TokenCategory category)
    {
        return category == TokenCategory.Word || category == TokenCategory.Dictionary || category == TokenCategory.Prefix;
    }

    private static CaseStyle ResolveStyle(TokenList tokens, TargetWordInfo targetWords, CaseAnalysisOptions options)
    {
        var matches = BuildMatchSet(tokens, targetWords);
        return ResolveStyleFromMatches(
            matches,
            targetWords.TargetWordCount,
            targetWords.HasUnderscoreSeparator,
            tokens.GetSpan(tokens[targetWords.FirstTargetWordIndex]),
            options);
    }

    private static CaseStyle ResolveStyle(ReadOnlySpan<char> source, TokenList tokens, TargetWordInfo targetWords, CaseAnalysisOptions options)
    {
        var matches = BuildMatchSet(source, tokens, targetWords);
        var firstTargetWord = tokens[targetWords.FirstTargetWordIndex];
        return ResolveStyleFromMatches(
            matches,
            targetWords.TargetWordCount,
            targetWords.HasUnderscoreSeparator,
            source.Slice(firstTargetWord.Start, firstTargetWord.Length),
            options);
    }

    private static CaseStyleMatchSet BuildMatchSet(TokenList tokens, TargetWordInfo targetWords)
    {
        if (targetWords.TargetWordCount == 0)
        {
            return default;
        }

        if (targetWords.HasUnderscoreSeparator)
        {
            return new CaseStyleMatchSet(
                pascalCase: false,
                camelCase: false,
                upperSnakeCase: AllWordsPascal(tokens, targetWords),
                lowerSnakeCase: AllWordsLower(tokens, targetWords),
                screamingSnakeCase: AllWordsUpper(tokens, targetWords));
        }

        var firstWordSpan = tokens.GetSpan(tokens[targetWords.FirstTargetWordIndex]);
        if (firstWordSpan.Length == 0)
        {
            return default;
        }

        var startsUpper = char.IsUpper(firstWordSpan[0]);
        var startsLower = char.IsLower(firstWordSpan[0]);

        if (targetWords.TargetWordCount > 1)
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

    private static CaseStyleMatchSet BuildMatchSet(ReadOnlySpan<char> source, TokenList tokens, TargetWordInfo targetWords)
    {
        if (targetWords.TargetWordCount == 0)
        {
            return default;
        }

        if (targetWords.HasUnderscoreSeparator)
        {
            return new CaseStyleMatchSet(
                pascalCase: false,
                camelCase: false,
                upperSnakeCase: AllWordsPascal(source, tokens, targetWords),
                lowerSnakeCase: AllWordsLower(source, tokens, targetWords),
                screamingSnakeCase: AllWordsUpper(source, tokens, targetWords));
        }

        var firstTargetWord = tokens[targetWords.FirstTargetWordIndex];
        var firstWordSpan = source.Slice(firstTargetWord.Start, firstTargetWord.Length);
        if (firstWordSpan.Length == 0)
        {
            return default;
        }

        var startsUpper = char.IsUpper(firstWordSpan[0]);
        var startsLower = char.IsLower(firstWordSpan[0]);

        if (targetWords.TargetWordCount > 1)
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
        var isPascal = IsPascalWord(text);
        return new CaseStyleMatchSet(
            pascalCase: isPascal,
            camelCase: startsLower,
            upperSnakeCase: isPascal,
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

    private static bool AllWordsLower(TokenList tokens, TargetWordInfo info)
    {
        var matched = 0;
        var seenWordLike = 0;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!IsWordLike(token.Category))
            {
                continue;
            }

            if (seenWordLike < info.SkipWordLikeCount)
            {
                seenWordLike++;
                continue;
            }

            if (!ContainsLetterWithCase(tokens.GetSpan(token), isUpper: false))
            {
                return false;
            }

            matched++;
            if (matched == info.TargetWordCount)
            {
                return true;
            }
        }

        return matched == info.TargetWordCount;
    }

    private static bool AllWordsLower(ReadOnlySpan<char> source, TokenList tokens, TargetWordInfo info)
    {
        var matched = 0;
        var seenWordLike = 0;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!IsWordLike(token.Category))
            {
                continue;
            }

            if (seenWordLike < info.SkipWordLikeCount)
            {
                seenWordLike++;
                continue;
            }

            if (!ContainsLetterWithCase(source.Slice(token.Start, token.Length), isUpper: false))
            {
                return false;
            }

            matched++;
            if (matched == info.TargetWordCount)
            {
                return true;
            }
        }

        return matched == info.TargetWordCount;
    }

    private static bool AllWordsUpper(TokenList tokens, TargetWordInfo info)
    {
        var matched = 0;
        var seenWordLike = 0;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!IsWordLike(token.Category))
            {
                continue;
            }

            if (seenWordLike < info.SkipWordLikeCount)
            {
                seenWordLike++;
                continue;
            }

            if (!ContainsLetterWithCase(tokens.GetSpan(token), isUpper: true))
            {
                return false;
            }

            matched++;
            if (matched == info.TargetWordCount)
            {
                return true;
            }
        }

        return matched == info.TargetWordCount;
    }

    private static bool AllWordsUpper(ReadOnlySpan<char> source, TokenList tokens, TargetWordInfo info)
    {
        var matched = 0;
        var seenWordLike = 0;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!IsWordLike(token.Category))
            {
                continue;
            }

            if (seenWordLike < info.SkipWordLikeCount)
            {
                seenWordLike++;
                continue;
            }

            if (!ContainsLetterWithCase(source.Slice(token.Start, token.Length), isUpper: true))
            {
                return false;
            }

            matched++;
            if (matched == info.TargetWordCount)
            {
                return true;
            }
        }

        return matched == info.TargetWordCount;
    }

    private static bool AllWordsPascal(TokenList tokens, TargetWordInfo info)
    {
        var matched = 0;
        var seenWordLike = 0;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!IsWordLike(token.Category))
            {
                continue;
            }

            if (seenWordLike < info.SkipWordLikeCount)
            {
                seenWordLike++;
                continue;
            }

            if (!IsPascalWord(tokens.GetSpan(token)))
            {
                return false;
            }

            matched++;
            if (matched == info.TargetWordCount)
            {
                return true;
            }
        }

        return matched == info.TargetWordCount;
    }

    private static bool AllWordsPascal(ReadOnlySpan<char> source, TokenList tokens, TargetWordInfo info)
    {
        var matched = 0;
        var seenWordLike = 0;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!IsWordLike(token.Category))
            {
                continue;
            }

            if (seenWordLike < info.SkipWordLikeCount)
            {
                seenWordLike++;
                continue;
            }

            if (!IsPascalWord(source.Slice(token.Start, token.Length)))
            {
                return false;
            }

            matched++;
            if (matched == info.TargetWordCount)
            {
                return true;
            }
        }

        return matched == info.TargetWordCount;
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

    private readonly struct TargetWordInfo
    {
        public TargetWordInfo(int firstTargetWordIndex, int targetWordCount, bool isPrefixed, bool hasUnderscoreSeparator)
        {
            FirstTargetWordIndex = firstTargetWordIndex;
            TargetWordCount = targetWordCount;
            IsPrefixed = isPrefixed;
            HasUnderscoreSeparator = hasUnderscoreSeparator;
        }

        public int FirstTargetWordIndex { get; }

        public int TargetWordCount { get; }

        public bool IsPrefixed { get; }

        public bool HasUnderscoreSeparator { get; }

        public int SkipWordLikeCount => IsPrefixed ? 1 : 0;
    }
}
