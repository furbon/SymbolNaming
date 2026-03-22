using SymbolNaming.Dictionaries;
using SymbolNaming.Tokens;

namespace SymbolNaming.Analysis;

public sealed class DefaultCaseClassifier : ICaseClassifier
{
    public CaseClassificationResult Classify(TokenList tokens, CaseAnalysisOptions? options = null)
    {
        return TryClassify(tokens, out var result, options)
            ? result
            : CaseClassificationResult.Unknown;
    }

    public CaseClassificationResult Classify(ReadOnlySpan<char> source, TokenList tokens, CaseAnalysisOptions? options = null)
    {
        return TryClassify(source, tokens, out var result, options)
            ? result
            : CaseClassificationResult.Unknown;
    }

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

        if (!tokens.HasSource)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        var words = new List<Token>(tokens.Count);

        foreach (var token in tokens)
        {
            if (token.Category == TokenCategory.Word || token.Category == TokenCategory.Dictionary || token.Category == TokenCategory.Prefix)
            {
                words.Add(token);
            }
        }

        if (words.Count == 0)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        var isPrefixed = words.Count >= 2 && IsPrefixToken(words[0], tokens, options);
        List<Token> targetWords;

        if (isPrefixed)
        {
            targetWords = new List<Token>(words.Count - 1);
            for (var i = 1; i < words.Count; i++)
            {
                targetWords.Add(words[i]);
            }
        }
        else
        {
            targetWords = words;
        }

        var hasUnderscoreSeparator = HasMeaningfulUnderscoreSeparator(tokens, options);

        if (targetWords.Count == 0)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        if (hasUnderscoreSeparator)
        {
            if (AllWordsLower(targetWords, tokens))
            {
                result = new CaseClassificationResult(CaseStyle.LowerSnakeCase, isPrefixed);
                return true;
            }

            if (AllWordsPascal(targetWords, tokens))
            {
                result = new CaseClassificationResult(CaseStyle.UpperSnakeCase, isPrefixed);
                return true;
            }

            if (AllWordsUpper(targetWords, tokens))
            {
                result = new CaseClassificationResult(CaseStyle.ScreamingSnakeCase, isPrefixed);
                return true;
            }

            result = CaseClassificationResult.Unknown;
            return false;
        }

        var firstWord = targetWords[0];
        var firstWordSpan = tokens.GetSpan(firstWord);
        if (firstWordSpan.Length == 0)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        var startsUpper = char.IsUpper(firstWordSpan[0]);
        var startsLower = char.IsLower(firstWordSpan[0]);

        if (startsUpper)
        {
            result = new CaseClassificationResult(CaseStyle.PascalCase, isPrefixed);
            return true;
        }

        if (!startsLower)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        result = new CaseClassificationResult(CaseStyle.CamelCase, isPrefixed);
        return true;
    }

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

        var words = new List<Token>(tokens.Count);

        foreach (var token in tokens)
        {
            if (token.Category == TokenCategory.Word || token.Category == TokenCategory.Dictionary || token.Category == TokenCategory.Prefix)
            {
                words.Add(token);
            }
        }

        if (words.Count == 0)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        var isPrefixed = words.Count >= 2 && IsPrefixToken(words[0], source, options);
        List<Token> targetWords;

        if (isPrefixed)
        {
            targetWords = new List<Token>(words.Count - 1);
            for (var i = 1; i < words.Count; i++)
            {
                targetWords.Add(words[i]);
            }
        }
        else
        {
            targetWords = words;
        }

        var hasUnderscoreSeparator = HasMeaningfulUnderscoreSeparator(tokens, source, options);

        if (targetWords.Count == 0)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        if (hasUnderscoreSeparator)
        {
            if (AllWordsLower(targetWords, source))
            {
                result = new CaseClassificationResult(CaseStyle.LowerSnakeCase, isPrefixed);
                return true;
            }

            if (AllWordsPascal(targetWords, source))
            {
                result = new CaseClassificationResult(CaseStyle.UpperSnakeCase, isPrefixed);
                return true;
            }

            if (AllWordsUpper(targetWords, source))
            {
                result = new CaseClassificationResult(CaseStyle.ScreamingSnakeCase, isPrefixed);
                return true;
            }

            result = CaseClassificationResult.Unknown;
            return false;
        }

        var firstWord = targetWords[0];
        var firstWordSpan = source.Slice(firstWord.Start, firstWord.Length);
        if (firstWordSpan.Length == 0)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        var startsUpper = char.IsUpper(firstWordSpan[0]);
        var startsLower = char.IsLower(firstWordSpan[0]);

        if (startsUpper)
        {
            result = new CaseClassificationResult(CaseStyle.PascalCase, isPrefixed);
            return true;
        }

        if (!startsLower)
        {
            result = CaseClassificationResult.Unknown;
            return false;
        }

        result = new CaseClassificationResult(CaseStyle.CamelCase, isPrefixed);
        return true;
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
