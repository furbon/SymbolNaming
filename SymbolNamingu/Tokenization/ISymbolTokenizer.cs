using SymbolNaming.Tokens;

namespace SymbolNaming.Tokenization;

public interface ISymbolTokenizer
{
    TokenList Tokenize(ReadOnlySpan<char> input);

    TokenList Tokenize(string input);
}
