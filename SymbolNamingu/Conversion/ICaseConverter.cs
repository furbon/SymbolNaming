using SymbolNaming.Tokens;

namespace SymbolNaming.Conversion;

public interface ICaseConverter
{
    string Convert(TokenList tokens, CaseStyle targetStyle, CaseConversionOptions? options = null);
}
