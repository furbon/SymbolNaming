using SymbolNaming.Tokens;

namespace SymbolNaming.Analysis;

/// <summary>
/// トークン列から命名スタイルを分類するインターフェイスです。
/// </summary>
public interface ICaseClassifier
{
    /// <summary>
    /// トークン列を分類し、分類不能時は <see cref="CaseClassificationResult.Unknown"/> を返します。
    /// </summary>
    CaseClassificationResult Classify(TokenList tokens, CaseAnalysisOptions? options = null);

    /// <summary>
    /// トークン列の分類を試行します。
    /// </summary>
    bool TryClassify(TokenList tokens, out CaseClassificationResult result, CaseAnalysisOptions? options = null);
}
