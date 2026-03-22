using SymbolNaming.Analysis;
using SymbolNaming.Conversion;
using SymbolNaming.Tokenization;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

public sealed class SymbolCaseEngine
{
    private readonly ISymbolTokenizer _tokenizer;
    private readonly ICaseClassifier _classifier;
    private readonly ICaseConverter _converter;

    public SymbolCaseEngine(ISymbolTokenizer tokenizer, ICaseClassifier classifier, ICaseConverter converter)
    {
        _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public TokenList Tokenize(string input)
    {
        return _tokenizer.Tokenize(input);
    }

    public TokenList Tokenize(ReadOnlySpan<char> input)
    {
        return _tokenizer.Tokenize(input);
    }

    public CaseClassificationResult Analyze(string input, CaseAnalysisOptions? options = null)
    {
        var tokens = _tokenizer.Tokenize(input);
        return _classifier.Classify(tokens, options);
    }

    public bool TryAnalyze(string input, out CaseClassificationResult result, CaseAnalysisOptions? options = null)
    {
        var tokens = _tokenizer.Tokenize(input);
        return _classifier.TryClassify(tokens, out result, options);
    }

    public SymbolInspection Inspect(string input, CaseAnalysisOptions? options = null)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var tokens = _tokenizer.Tokenize(input);
        var classification = _classifier.Classify(tokens, options);
        return new SymbolInspection(input, tokens, classification);
    }

    public SymbolInspectionSpan Inspect(ReadOnlySpan<char> input, CaseAnalysisOptions? options = null)
    {
        var tokens = _tokenizer.Tokenize(input);
        var classification = ClassifySpanInput(input, tokens, options);
        return new SymbolInspectionSpan(input, tokens, classification);
    }

    public string Convert(string input, CaseStyle targetStyle, CaseConversionOptions? options = null)
    {
        var tokens = _tokenizer.Tokenize(input);
        return _converter.Convert(tokens, targetStyle, options);
    }

    public string Convert(TokenList tokens, CaseStyle targetStyle, CaseConversionOptions? options = null)
    {
        return _converter.Convert(tokens, targetStyle, options);
    }

    private CaseClassificationResult ClassifySpanInput(ReadOnlySpan<char> input, TokenList tokens, CaseAnalysisOptions? options)
    {
        if (_classifier is DefaultCaseClassifier defaultClassifier)
        {
            return defaultClassifier.Classify(input, tokens, options);
        }

        var sourceText = input.ToString();
        var sourceTokens = _tokenizer.Tokenize(sourceText);
        return _classifier.Classify(sourceTokens, options);
    }
}
