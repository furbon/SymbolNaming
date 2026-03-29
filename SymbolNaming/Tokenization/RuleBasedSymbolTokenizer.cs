using SymbolNaming.Tokenization.SplitRules;
using SymbolNaming.Dictionaries;
using SymbolNaming.Lifecycle;
using SymbolNaming.Tokens;

namespace SymbolNaming.Tokenization;

/// <summary>
/// 分割ルールに基づく既定のトークナイザーです。
/// </summary>
public sealed class RuleBasedSymbolTokenizer : ISymbolTokenizer, IFreezableComponent
{
    private const int ReservedTokenCapacity = 8;
    private const int InitialRuleCapacity = 8;

    private readonly List<ISplitRule> _rules;
    private readonly IProtectedWordProvider _protectedWordProvider;
    private readonly IPrefixProvider _prefixProvider;
    private readonly object _freezeSync = new();

    private ISplitRule[]? _frozenRules;

    /// <summary>
    /// 空ルールで初期化します。
    /// </summary>
    public RuleBasedSymbolTokenizer()
    {
        _rules = new List<ISplitRule>(InitialRuleCapacity);
        _protectedWordProvider = EmptyProtectedWordProvider.Instance;
        _prefixProvider = EmptyPrefixProvider.Instance;
    }

    /// <summary>
    /// 指定ルールで初期化します。
    /// </summary>
    public RuleBasedSymbolTokenizer(IEnumerable<ISplitRule> rules)
    {
        _rules = CreateRuleList(rules);
        _protectedWordProvider = EmptyProtectedWordProvider.Instance;
        _prefixProvider = EmptyPrefixProvider.Instance;
    }

    /// <summary>
    /// ルールと辞書系プロバイダーを指定して初期化します。
    /// </summary>
    public RuleBasedSymbolTokenizer(IEnumerable<ISplitRule> rules, IProtectedWordProvider protectedWordProvider, IPrefixProvider prefixProvider)
    {
        _rules = CreateRuleList(rules);
        _protectedWordProvider = protectedWordProvider ?? throw new ArgumentNullException(nameof(protectedWordProvider));
        _prefixProvider = prefixProvider ?? throw new ArgumentNullException(nameof(prefixProvider));
    }

    /// <summary>
    /// 状態が凍結済みかどうかを取得します。
    /// </summary>
    public bool IsFrozen => _frozenRules is not null;

    /// <summary>
    /// ルール構成を凍結し、以後の変更を禁止します。
    /// </summary>
    public void Freeze()
    {
        if (IsFrozen)
        {
            return;
        }

        lock (_freezeSync)
        {
            if (_frozenRules is not null)
            {
                return;
            }

            _frozenRules = _rules.ToArray();

            _rules.Clear();
            _rules.TrimExcess();
        }
    }

    /// <summary>
    /// 分割ルールを追加します。
    /// </summary>
    public void AddRule(ISplitRule rule)
    {
        EnsureNotFrozen();

        if (rule is null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        _rules.Add(rule);
    }

    /// <summary>
    /// 文字列をトークン化します。
    /// </summary>
    public TokenList Tokenize(string input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        EnsureFrozen();

        return TokenizeCore(input.AsSpan(), input);
    }

    /// <summary>
    /// 文字列 Span をトークン化します。
    /// </summary>
    public TokenList Tokenize(ReadOnlySpan<char> input)
    {
        EnsureFrozen();

        return TokenizeCore(input, null);
    }

    private TokenList TokenizeCore(ReadOnlySpan<char> input, string? sourceText)
    {
        var rules = _frozenRules;
        if (rules is null)
        {
            throw new InvalidOperationException("Tokenizer is not frozen. Call Freeze() before tokenization.");
        }

        var tokens = new List<Token>(ReservedTokenCapacity);

        var currentPosition = 0;
        var lastSplitPosition = 0;

        if (input.Length > 0 &&
            _prefixProvider.TryMatchLongest(input, start: 0, out var prefixLength) &&
            prefixLength > 0 &&
            prefixLength <= input.Length &&
            ShouldEmitLeadingPrefixToken(input.Slice(0, prefixLength)))
        {
            EmitToken(tokens, input, start: 0, length: prefixLength, TokenCategory.Prefix);
            currentPosition = prefixLength;
            lastSplitPosition = prefixLength;
        }

        while (currentPosition < input.Length)
        {
            var sliceFound = false;
            for (var i = 0; i < rules.Length; ++i)
            {
                var rule = rules[i];
                var splitResult = rule.Check(input, currentPosition);
                if (!splitResult.IsSplit)
                {
                    continue;
                }

                EmitToken(tokens, input, lastSplitPosition, currentPosition - lastSplitPosition, TokenCategory.Word);

                if (splitResult.Consumed)
                {
                    EmitToken(tokens, input, currentPosition, splitResult.ConsumeCount, splitResult.Category);

                    currentPosition += splitResult.ConsumeCount;
                    lastSplitPosition = currentPosition;
                }
                else
                {
                    lastSplitPosition = currentPosition;
                    ++currentPosition;
                }

                sliceFound = true;
                break;
            }

            if (!sliceFound)
            {
                ++currentPosition;
            }
        }

        EmitToken(tokens, input, lastSplitPosition, input.Length - lastSplitPosition, TokenCategory.Word);

        MarkSpecialCategories(tokens, input);

        return new TokenList(tokens, sourceText, takeOwnership: true);
    }

    private static void EmitToken(List<Token> tokens, ReadOnlySpan<char> input, int start, int length, TokenCategory category)
    {
        if (length <= 0)
        {
            return;
        }

        tokens.Add(new Token(start, length, category));
    }

    private static bool ShouldEmitLeadingPrefixToken(ReadOnlySpan<char> prefix)
    {
        for (var i = 0; i < prefix.Length; ++i)
        {
            if (!char.IsLetterOrDigit(prefix[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static List<ISplitRule> CreateRuleList(IEnumerable<ISplitRule> rules)
    {
        if (rules is null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        var ruleList = rules.ToList();
        for (var i = 0; i < ruleList.Count; ++i)
        {
            if (ruleList[i] is null)
            {
                throw new ArgumentException("Rules collection cannot contain null items.", nameof(rules));
            }
        }

        return ruleList;
    }

    private void MarkSpecialCategories(List<Token> tokens, ReadOnlySpan<char> input)
    {
        var firstWordIndex = -1;
        for (var i = 0; i < tokens.Count; ++i)
        {
            if (tokens[i].Category == TokenCategory.Word)
            {
                firstWordIndex = i;
                break;
            }
        }

        for (var i = 0; i < tokens.Count; ++i)
        {
            var token = tokens[i];
            if (token.Category != TokenCategory.Word)
            {
                continue;
            }

            var category = token.Category;

            var tokenSpan = input.Slice(token.Start, token.Length);

            if (_protectedWordProvider.IsProtected(tokenSpan))
            {
                category = TokenCategory.Dictionary;
            }

            if (category != TokenCategory.Dictionary && i == firstWordIndex && _prefixProvider.IsPrefix(tokenSpan))
            {
                category = TokenCategory.Prefix;
            }

            if (category != token.Category)
            {
                tokens[i] = new Token(token.Start, token.Length, category);
            }
        }
    }

    private void EnsureFrozen()
    {
        if (!IsFrozen)
        {
            throw new InvalidOperationException("Tokenizer is not frozen. Call Freeze() before tokenization.");
        }
    }

    private void EnsureNotFrozen()
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException("Tokenizer is already frozen and cannot be modified.");
        }
    }
}
