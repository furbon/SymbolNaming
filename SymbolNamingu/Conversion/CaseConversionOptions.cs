namespace SymbolNaming.Conversion;

public sealed class CaseConversionOptions
{
    public PrefixPolicy PrefixPolicy { get; set; } = PrefixPolicy.Keep;

    public string? PrefixToAdd { get; set; }

    public AcronymPolicy AcronymPolicy { get; set; } = AcronymPolicy.Preserve;
}
