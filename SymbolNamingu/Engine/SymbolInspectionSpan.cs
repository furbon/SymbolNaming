using SymbolNaming.Analysis;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

public readonly ref struct SymbolInspectionSpan
{
    private readonly ReadOnlySpan<char> _source;
    private readonly SymbolInspectionSliceInfo _sliceInfo;

    internal SymbolInspectionSpan(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification)
    {
        _source = source;
        Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        Classification = classification;
        _sliceInfo = SymbolInspectionSliceInfoFactory.Create(source, tokens, classification);
    }

    public ReadOnlySpan<char> Source => _source;

    public TokenList Tokens { get; }

    public CaseClassificationResult Classification { get; }

    public CaseStyle CaseStyle => Classification.Style;

    public bool Prefixed => Classification.Prefixed;

    public bool HasPrefix => _sliceInfo.PrefixLength > 0;

    public ReadOnlySpan<char> Prefix =>
        HasPrefix
            ? _source.Slice(_sliceInfo.PrefixStart, _sliceInfo.PrefixLength)
            : ReadOnlySpan<char>.Empty;

    public ReadOnlySpan<char> SymbolNameWithoutPrefix => _source.Slice(_sliceInfo.SymbolStart);
}
