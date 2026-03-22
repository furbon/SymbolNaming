using SymbolNaming.Tokens;

namespace SymbolNaming.Analysis;

public interface ICaseClassifier
{
    CaseClassificationResult Classify(TokenList tokens, CaseAnalysisOptions? options = null);

    bool TryClassify(TokenList tokens, out CaseClassificationResult result, CaseAnalysisOptions? options = null);
}
