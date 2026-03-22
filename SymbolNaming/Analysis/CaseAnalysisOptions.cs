using SymbolNaming.Dictionaries;

namespace SymbolNaming.Analysis;

/// <summary>
/// Case 判定時のオプションを表します。
/// </summary>
public sealed class CaseAnalysisOptions
{
    /// <summary>
    /// プレフィックス判定に使用するプロバイダーです。
    /// </summary>
    public IPrefixProvider PrefixProvider { get; set; } = EmptyPrefixProvider.Instance;

    /// <summary>
    /// 保護語（辞書語）判定に使用するプロバイダーです。
    /// </summary>
    public IProtectedWordProvider ProtectedWordProvider { get; set; } = EmptyProtectedWordProvider.Instance;

    /// <summary>
    /// 頭字語を 1 語として扱うかどうかを示す互換オプションです。
    /// </summary>
    public bool TreatAcronymsAsSingleWord { get; set; } = true;
}
