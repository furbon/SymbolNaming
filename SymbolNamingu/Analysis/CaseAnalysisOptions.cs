using SymbolNaming.Dictionaries;

namespace SymbolNaming.Analysis;

public sealed class CaseAnalysisOptions
{
    public IPrefixProvider PrefixProvider { get; set; } = EmptyPrefixProvider.Instance;

    public IProtectedWordProvider ProtectedWordProvider { get; set; } = EmptyProtectedWordProvider.Instance;

    public bool TreatAcronymsAsSingleWord { get; set; } = true;
}
