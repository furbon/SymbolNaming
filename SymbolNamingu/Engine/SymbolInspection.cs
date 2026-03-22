using SymbolNaming.Analysis;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

public sealed class SymbolInspection
{
    public SymbolInspection(string source, TokenList tokens, CaseClassificationResult classification)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        Classification = classification;

        var sliceInfo = SymbolInspectionSliceInfoFactory.Create(source.AsSpan(), tokens, classification);
        HasPrefix = sliceInfo.PrefixLength > 0;
        Prefix = HasPrefix ? source.Substring(sliceInfo.PrefixStart, sliceInfo.PrefixLength) : null;
        SymbolNameWithoutPrefix = source.Substring(sliceInfo.SymbolStart);
    }

    public string Source { get; }

    public TokenList Tokens { get; }

    public CaseClassificationResult Classification { get; }

    public CaseStyle CaseStyle => Classification.Style;

    public bool Prefixed => Classification.Prefixed;

    public bool HasPrefix { get; }

    public string? Prefix { get; }

    public string SymbolNameWithoutPrefix { get; }
}
