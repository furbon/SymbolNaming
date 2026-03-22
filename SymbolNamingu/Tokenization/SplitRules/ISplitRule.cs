using SymbolNaming.Tokens;

namespace SymbolNaming.Tokenization.SplitRules;

public interface ISplitRule
{
    SplitResult Check(ReadOnlySpan<char> span, int index);
}
