namespace SymbolNaming.Analysis;

/// <summary>
/// 複合サフィックスルールの実行時最適化用インターフェイスです。
/// </summary>
internal interface ICompositeSuffixPatternRuleRuntime
{
    /// <summary>
    /// 文字列化済みのベース名・サフィックスで一致判定します。
    /// </summary>
    bool IsMatch(string baseName, string suffix);
}
