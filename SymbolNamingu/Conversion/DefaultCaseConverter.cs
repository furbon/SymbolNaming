using SymbolNaming.Tokens;
using System.Buffers;

namespace SymbolNaming.Conversion;

public sealed class DefaultCaseConverter : ICaseConverter
{
    public string Convert(TokenList tokens, CaseStyle targetStyle, CaseConversionOptions? options = null)
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

        var sourceWords = new List<string>(tokens.Count);
        string? existingPrefix = null;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!IsWordLike(token.Category))
            {
                continue;
            }

            if (existingPrefix is null && token.Category == TokenCategory.Prefix)
            {
                existingPrefix = tokens.GetSpan(token).ToString();

                var nextIndex = i + 1;
                if (nextIndex < tokens.Count && IsUnderscoreSeparator(tokens[nextIndex], tokens))
                {
                    existingPrefix += "_";
                }

                continue;
            }

            sourceWords.Add(tokens.GetSpan(token).ToString());
        }

        if (sourceWords.Count == 0)
        {
            return options.PrefixPolicy == PrefixPolicy.Add
                ? options.PrefixToAdd ?? string.Empty
                : string.Empty;
        }

        var convertedBody = ConvertWords(sourceWords, targetStyle, options.AcronymPolicy);
        var prefix = ResolvePrefix(options, existingPrefix);

        return prefix + convertedBody;
    }

    private static string ResolvePrefix(CaseConversionOptions options, string? existingPrefix)
    {
        return options.PrefixPolicy switch
        {
            PrefixPolicy.Keep => existingPrefix ?? string.Empty,
            PrefixPolicy.Remove => string.Empty,
            PrefixPolicy.Add => options.PrefixToAdd ?? string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(options.PrefixPolicy), options.PrefixPolicy, "Unsupported prefix policy."),
        };
    }

    private static string ConvertWords(IReadOnlyList<string> words, CaseStyle targetStyle, AcronymPolicy acronymPolicy)
    {
        switch (targetStyle)
        {
            case CaseStyle.PascalCase:
                return BuildPascalCase(words, acronymPolicy);

            case CaseStyle.CamelCase:
                return BuildCamelCase(words, acronymPolicy);

            case CaseStyle.UpperSnakeCase:
                return BuildSnakeCase(words, static (word, policy) => ToPascalWord(word, policy), acronymPolicy);

            case CaseStyle.LowerSnakeCase:
                return BuildSnakeCase(words, static (word, _) => ToLowerWord(word), acronymPolicy);

            case CaseStyle.ScreamingSnakeCase:
                return BuildSnakeCase(words, static (word, _) => ToUpperWord(word), acronymPolicy);

            default:
                throw new ArgumentOutOfRangeException(nameof(targetStyle), targetStyle, "Unsupported target style.");
        }
    }

    private static string BuildPascalCase(IReadOnlyList<string> words, AcronymPolicy acronymPolicy)
    {
        if (words.Count == 0)
        {
            return string.Empty;
        }

        var converted = new string[words.Count];
        for (var i = 0; i < words.Count; i++)
        {
            converted[i] = ToPascalWord(words[i], acronymPolicy);
        }

        return string.Concat(converted);
    }

    private static string BuildCamelCase(IReadOnlyList<string> words, AcronymPolicy acronymPolicy)
    {
        if (words.Count == 0)
        {
            return string.Empty;
        }

        var converted = new string[words.Count];
        converted[0] = ToCamelFirstWord(words[0], acronymPolicy);
        for (var i = 1; i < words.Count; i++)
        {
            converted[i] = ToPascalWord(words[i], acronymPolicy);
        }

        return string.Concat(converted);
    }

    private static string BuildSnakeCase(
        IReadOnlyList<string> words,
        Func<string, AcronymPolicy, string> wordConverter,
        AcronymPolicy acronymPolicy)
    {
        if (words.Count == 0)
        {
            return string.Empty;
        }

        var converted = new string[words.Count];
        for (var i = 0; i < words.Count; i++)
        {
            converted[i] = wordConverter(words[i], acronymPolicy);
        }

        return string.Join("_", converted);
    }

    private static bool IsWordLike(TokenCategory category)
    {
        return category == TokenCategory.Word
            || category == TokenCategory.Dictionary
            || category == TokenCategory.Prefix;
    }

    private static bool IsUnderscoreSeparator(Token token, TokenList tokens)
    {
        if (token.Category != TokenCategory.Separator)
        {
            return false;
        }

        var span = tokens.GetSpan(token);
        return span.Length == 1 && span[0] == '_';
    }

    private static string ToPascalWord(string word, AcronymPolicy acronymPolicy)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        if (acronymPolicy == AcronymPolicy.Preserve && IsAcronymWord(word))
        {
            return word.ToUpperInvariant();
        }

        return char.ToUpperInvariant(word[0]) + ToLowerWord(word.AsSpan(1));
    }

    private static string ToCamelFirstWord(string word, AcronymPolicy acronymPolicy)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        if (acronymPolicy == AcronymPolicy.Preserve && IsAcronymWord(word))
        {
            return ToLowerWord(word);
        }

        return ToLowerWord(word);
    }

    private static bool IsAcronymWord(string word)
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

    private static string ToLowerWord(string word)
    {
        return ToLowerWord(word.AsSpan());
    }

    private static string ToLowerWord(ReadOnlySpan<char> word)
    {
        return ConvertInvariant(word, toUpper: false);
    }

    private static string ToUpperWord(string word)
    {
        return ConvertInvariant(word.AsSpan(), toUpper: true);
    }

    private static string ConvertInvariant(ReadOnlySpan<char> value, bool toUpper)
    {
        if (value.IsEmpty)
        {
            return string.Empty;
        }

        var buffer = ArrayPool<char>.Shared.Rent(value.Length);

        try
        {
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                buffer[i] = toUpper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);
            }

            return new string(buffer, 0, value.Length);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }
}
