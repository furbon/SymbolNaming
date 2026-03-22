namespace SymbolNaming.Analysis;

public readonly struct CaseClassificationResult
{
    public static CaseClassificationResult Unknown { get; } = new(CaseStyle.Unknown, prefixed: false);

    public CaseClassificationResult(CaseStyle style, bool prefixed)
    {
        Style = style;
        Prefixed = prefixed;
    }

    public CaseStyle Style { get; }

    public bool Prefixed { get; }
}
