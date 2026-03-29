using SymbolNaming.Dictionaries;
using SymbolNaming.Tokenization.SplitRules;

namespace SymbolNaming.Tokenization;

/// <summary>
/// 標準トークナイザー構成を提供するファクトリです。
/// </summary>
public static class SymbolTokenizerFactory
{
    private static readonly Type[] DefaultRuleTypes =
    {
        typeof(VerbatimRule),
        typeof(UpperCaseRule),
        typeof(PostDigitRule),
        typeof(UnderscoreRule),
    };

    /// <summary>
    /// 既定ルール・既定辞書構成のトークナイザーを生成します。
    /// </summary>
    public static RuleBasedSymbolTokenizer CreateDefault(
        IProtectedWordProvider? protectedWordProvider = null,
        IPrefixProvider? prefixProvider = null)
    {
        var tokenizer = new RuleBasedSymbolTokenizer(
            CreateDefaultRules(),
            protectedWordProvider ?? EmptyProtectedWordProvider.Instance,
            prefixProvider ?? EmptyPrefixProvider.Instance);

        tokenizer.Freeze();
        return tokenizer;
    }

    /// <summary>
    /// 既定分割ルールの新規インスタンス列を返します。
    /// </summary>
    public static IReadOnlyList<ISplitRule> CreateDefaultRules()
    {
        var rules = new ISplitRule[DefaultRuleTypes.Length];
        for (var i = 0; i < DefaultRuleTypes.Length; ++i)
        {
            rules[i] = (ISplitRule)Activator.CreateInstance(DefaultRuleTypes[i])!;
        }

        return rules;
    }
}
