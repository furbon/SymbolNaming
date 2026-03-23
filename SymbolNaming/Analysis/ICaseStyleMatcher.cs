using SymbolNaming.Tokens;

namespace SymbolNaming.Analysis;

/// <summary>
/// トークン列に対するスタイル適合判定を提供するインターフェイスです。
/// </summary>
public interface ICaseStyleMatcher
{
    /// <summary>
    /// 指定スタイルに適合するかどうかを判定します。
    /// </summary>
    bool IsMatch(TokenList tokens, CaseStyle style, CaseAnalysisOptions? options = null);

    /// <summary>
    /// 適合するスタイル候補の集合を取得します。
    /// </summary>
    CaseStyleMatchSet GetMatches(TokenList tokens, CaseAnalysisOptions? options = null);
}
