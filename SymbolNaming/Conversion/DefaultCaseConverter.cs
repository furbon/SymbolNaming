using SymbolNaming.Tokens;
using System.Buffers;

namespace SymbolNaming.Conversion;

/// <summary>
/// 既定の Case 変換器です。
/// </summary>
public sealed class DefaultCaseConverter : ICaseConverter
{
    /// <summary>
    /// トークン列を指定スタイルへ変換します。
    /// </summary>
    /// <remarks>
    /// プレフィックスは <see cref="CaseConversionOptions.PrefixPolicy"/> に従って処理されます。
    /// </remarks>
    public CaseConversionResult Convert(TokenList tokens, CaseStyle targetStyle, CaseConversionOptions? options = null)
    {
        if (tokens is null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        if (!tokens.HasSource)
        {
            throw new InvalidOperationException("Source text is not available for conversion.");
        }

        if (targetStyle == CaseStyle.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(targetStyle), targetStyle, "Unknown style cannot be used as conversion target.");
        }

        options ??= new CaseConversionOptions();
        List<CaseConversionWarning>? warnings = null;

        var source = tokens.SourceSpan;
        Span<Token> wordTokens = stackalloc Token[16];
        Token[]? rentedWordTokens = null;
        var wordCount = 0;
        var hasExistingPrefix = false;
        var existingPrefixToken = default(Token);
        var existingPrefixHasTrailingUnderscore = false;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!IsWordLike(token.Category))
            {
                continue;
            }

            if (!hasExistingPrefix && token.Category == TokenCategory.Prefix)
            {
                hasExistingPrefix = true;
                existingPrefixToken = token;

                var nextIndex = i + 1;
                if (nextIndex < tokens.Count && IsUnderscoreSeparator(tokens[nextIndex], source))
                {
                    existingPrefixHasTrailingUnderscore = true;
                }

                continue;
            }

            if (wordCount == wordTokens.Length)
            {
                GrowWordTokenBuffer(ref wordTokens, ref rentedWordTokens, wordCount);
            }

            wordTokens[wordCount++] = token;
        }

        try
        {
            if (wordCount == 0)
            {
                warnings = AddWarning(warnings, CaseConversionWarning.NoWordToken);

                if (options.PrefixPolicy == PrefixPolicy.Add && string.IsNullOrEmpty(options.PrefixToAdd))
                {
                    warnings = AddWarning(warnings, CaseConversionWarning.EmptyPrefixToAdd);
                }

                var output = options.PrefixPolicy == PrefixPolicy.Add
                    ? options.PrefixToAdd ?? string.Empty
                    : string.Empty;

                return new CaseConversionResult(output, options.PrefixPolicy, options.AcronymPolicy, warnings);
            }

            var prefixLength = GetPrefixLength(options, hasExistingPrefix, existingPrefixToken, existingPrefixHasTrailingUnderscore);
            var bodyLength = GetBodyLength(wordTokens.Slice(0, wordCount), targetStyle);
            var outputLength = prefixLength + bodyLength;

            var outputBuffer = ArrayPool<char>.Shared.Rent(outputLength);

            try
            {
                var output = outputBuffer.AsSpan(0, outputLength);
                var written = WritePrefix(
                    output,
                    source,
                    options,
                    hasExistingPrefix,
                    existingPrefixToken,
                    existingPrefixHasTrailingUnderscore);

                written += WriteConvertedBody(
                    output.Slice(written),
                    source,
                    wordTokens.Slice(0, wordCount),
                    targetStyle,
                    options.AcronymPolicy);

                if (options.PrefixPolicy == PrefixPolicy.Add && string.IsNullOrEmpty(options.PrefixToAdd))
                {
                    warnings = AddWarning(warnings, CaseConversionWarning.EmptyPrefixToAdd);
                }

                return new CaseConversionResult(new string(outputBuffer, 0, written), options.PrefixPolicy, options.AcronymPolicy, warnings);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(outputBuffer);
            }
        }
        finally
        {
            if (rentedWordTokens is not null)
            {
                ArrayPool<Token>.Shared.Return(rentedWordTokens);
            }
        }
    }

    private static void GrowWordTokenBuffer(ref Span<Token> wordTokens, ref Token[]? rentedWordTokens, int count)
    {
        var newBuffer = ArrayPool<Token>.Shared.Rent(wordTokens.Length * 2);
        wordTokens.Slice(0, count).CopyTo(newBuffer);

        if (rentedWordTokens is not null)
        {
            ArrayPool<Token>.Shared.Return(rentedWordTokens);
        }

        rentedWordTokens = newBuffer;
        wordTokens = newBuffer;
    }

    private static List<CaseConversionWarning> AddWarning(List<CaseConversionWarning>? warnings, CaseConversionWarning warning)
    {
        warnings ??= new List<CaseConversionWarning>(2);
        warnings.Add(warning);
        return warnings;
    }

    private static int GetPrefixLength(CaseConversionOptions options, bool hasExistingPrefix, Token existingPrefixToken, bool existingPrefixHasTrailingUnderscore)
    {
        return options.PrefixPolicy switch
        {
            PrefixPolicy.Keep when hasExistingPrefix => existingPrefixToken.Length + (existingPrefixHasTrailingUnderscore ? 1 : 0),
            PrefixPolicy.Keep => 0,
            PrefixPolicy.Remove => 0,
            PrefixPolicy.Add => options.PrefixToAdd?.Length ?? 0,
            _ => throw new ArgumentOutOfRangeException(nameof(options.PrefixPolicy), options.PrefixPolicy, "Unsupported prefix policy."),
        };
    }

    private static int GetBodyLength(ReadOnlySpan<Token> words, CaseStyle targetStyle)
    {
        var length = 0;
        for (var i = 0; i < words.Length; i++)
        {
            length += words[i].Length;
        }

        if (IsSnakeStyle(targetStyle) && words.Length > 1)
        {
            length += words.Length - 1;
        }

        return length;
    }

    private static int WritePrefix(
        Span<char> destination,
        ReadOnlySpan<char> source,
        CaseConversionOptions options,
        bool hasExistingPrefix,
        Token existingPrefixToken,
        bool existingPrefixHasTrailingUnderscore)
    {
        switch (options.PrefixPolicy)
        {
            case PrefixPolicy.Keep:
                if (!hasExistingPrefix)
                {
                    return 0;
                }

                var prefixSpan = existingPrefixToken.AsSpan(source);
                prefixSpan.CopyTo(destination);
                var written = prefixSpan.Length;

                if (existingPrefixHasTrailingUnderscore)
                {
                    destination[written++] = '_';
                }

                return written;

            case PrefixPolicy.Remove:
                return 0;

            case PrefixPolicy.Add:
                var customPrefix = options.PrefixToAdd;
                if (string.IsNullOrEmpty(customPrefix))
                {
                    return 0;
                }

                customPrefix.AsSpan().CopyTo(destination);
                return customPrefix!.Length;

            default:
                throw new ArgumentOutOfRangeException(nameof(options.PrefixPolicy), options.PrefixPolicy, "Unsupported prefix policy.");
        }
    }

    private static int WriteConvertedBody(
        Span<char> destination,
        ReadOnlySpan<char> source,
        ReadOnlySpan<Token> words,
        CaseStyle targetStyle,
        AcronymPolicy acronymPolicy)
    {
        var written = 0;

        switch (targetStyle)
        {
            case CaseStyle.PascalCase:
                for (var i = 0; i < words.Length; i++)
                {
                    written += WritePascalWord(words[i].AsSpan(source), destination.Slice(written), acronymPolicy);
                }

                return written;

            case CaseStyle.CamelCase:
                written += WriteLowerWord(words[0].AsSpan(source), destination.Slice(written));
                for (var i = 1; i < words.Length; i++)
                {
                    written += WritePascalWord(words[i].AsSpan(source), destination.Slice(written), acronymPolicy);
                }

                return written;

            case CaseStyle.UpperSnakeCase:
                return WriteSnakeCase(destination, source, words, targetStyle, acronymPolicy);

            case CaseStyle.LowerSnakeCase:
                return WriteSnakeCase(destination, source, words, targetStyle, acronymPolicy);

            case CaseStyle.ScreamingSnakeCase:
                return WriteSnakeCase(destination, source, words, targetStyle, acronymPolicy);

            default:
                throw new ArgumentOutOfRangeException(nameof(targetStyle), targetStyle, "Unsupported target style.");
        }
    }

    private static int WriteSnakeCase(
        Span<char> destination,
        ReadOnlySpan<char> source,
        ReadOnlySpan<Token> words,
        CaseStyle targetStyle,
        AcronymPolicy acronymPolicy)
    {
        var written = 0;
        for (var i = 0; i < words.Length; i++)
        {
            if (i > 0)
            {
                destination[written++] = '_';
            }

            var word = words[i].AsSpan(source);
            var output = destination.Slice(written);
            switch (targetStyle)
            {
                case CaseStyle.UpperSnakeCase:
                    written += WritePascalWord(word, output, acronymPolicy);
                    break;

                case CaseStyle.LowerSnakeCase:
                    written += WriteLowerWord(word, output);
                    break;

                case CaseStyle.ScreamingSnakeCase:
                    written += WriteUpperWord(word, output);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(targetStyle), targetStyle, "Unsupported target style.");
            }
        }

        return written;
    }

    private static int WritePascalWord(ReadOnlySpan<char> word, Span<char> destination, AcronymPolicy acronymPolicy)
    {
        if (word.IsEmpty)
        {
            return 0;
        }

        if (acronymPolicy == AcronymPolicy.Preserve && IsAcronymWord(word))
        {
            return WriteUpperWord(word, destination);
        }

        destination[0] = char.ToUpperInvariant(word[0]);
        for (var i = 1; i < word.Length; i++)
        {
            destination[i] = char.ToLowerInvariant(word[i]);
        }

        return word.Length;
    }

    private static int WriteLowerWord(ReadOnlySpan<char> word, Span<char> destination)
    {
        for (var i = 0; i < word.Length; i++)
        {
            destination[i] = char.ToLowerInvariant(word[i]);
        }

        return word.Length;
    }

    private static int WriteUpperWord(ReadOnlySpan<char> word, Span<char> destination)
    {
        for (var i = 0; i < word.Length; i++)
        {
            destination[i] = char.ToUpperInvariant(word[i]);
        }

        return word.Length;
    }

    private static bool IsWordLike(TokenCategory category)
    {
        return category == TokenCategory.Word
            || category == TokenCategory.Dictionary
            || category == TokenCategory.Prefix;
    }

    private static bool IsUnderscoreSeparator(Token token, ReadOnlySpan<char> source)
    {
        if (token.Category != TokenCategory.Separator)
        {
            return false;
        }

        var span = token.AsSpan(source);
        return span.Length == 1 && span[0] == '_';
    }

    private static bool IsSnakeStyle(CaseStyle style)
    {
        return style == CaseStyle.UpperSnakeCase
            || style == CaseStyle.LowerSnakeCase
            || style == CaseStyle.ScreamingSnakeCase;
    }

    private static bool IsAcronymWord(ReadOnlySpan<char> word)
    {
        var hasLetter = false;

        foreach (var c in word)
        {
            if (!char.IsLetter(c))
            {
                continue;
            }

            hasLetter = true;
            if (!char.IsUpper(c))
            {
                return false;
            }
        }

        return hasLetter;
    }
}
