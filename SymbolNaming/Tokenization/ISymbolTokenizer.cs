using SymbolNaming.Tokens;

namespace SymbolNaming.Tokenization;

/// <summary>
/// シンボル名をトークン列に分割するインターフェイスです。
/// </summary>
public interface ISymbolTokenizer
{
    /// <summary>
    /// 文字列 Span をトークン化します。
    /// </summary>
    TokenList Tokenize(ReadOnlySpan<char> input);

    /// <summary>
    /// 文字列をトークン化します。
    /// </summary>
    TokenList Tokenize(string input);
}
