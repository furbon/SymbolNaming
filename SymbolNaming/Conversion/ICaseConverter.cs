using SymbolNaming.Tokens;

namespace SymbolNaming.Conversion;

/// <summary>
/// トークン列を指定の命名スタイルへ変換するインターフェイスです。
/// </summary>
public interface ICaseConverter
{
    /// <summary>
    /// トークン列を <paramref name="targetStyle"/> に変換します。
    /// </summary>
    CaseConversionResult Convert(TokenList tokens, CaseStyle targetStyle, CaseConversionOptions? options = null);
}
