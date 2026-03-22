using SymbolNaming.Analysis;
using SymbolNaming.Conversion;
using SymbolNaming.Tokenization;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

/// <summary>
/// Tokenize / Analyze / Convert を統合して提供するエンジンです。
/// </summary>
public sealed class SymbolCaseEngine
{
    private readonly ISymbolTokenizer _tokenizer;
    private readonly ICaseClassifier _classifier;
    private readonly ICaseConverter _converter;

    /// <summary>
    /// 依存コンポーネントを指定して初期化します。
    /// </summary>
    public SymbolCaseEngine(ISymbolTokenizer tokenizer, ICaseClassifier classifier, ICaseConverter converter)
    {
        _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    /// <summary>
    /// 文字列をトークン化します。
    /// </summary>
    public TokenList Tokenize(string input)
    {
        return _tokenizer.Tokenize(input);
    }

    /// <summary>
    /// 文字列 Span をトークン化します。
    /// </summary>
    public TokenList Tokenize(ReadOnlySpan<char> input)
    {
        return _tokenizer.Tokenize(input);
    }

    /// <summary>
    /// 入力文字列の命名スタイルを解析します。
    /// </summary>
    public CaseClassificationResult Analyze(string input, CaseAnalysisOptions? options = null)
    {
        var tokens = _tokenizer.Tokenize(input);
        return _classifier.Classify(tokens, options);
    }

    /// <summary>
    /// 入力文字列の命名スタイル解析を試行します。
    /// </summary>
    public bool TryAnalyze(string input, out CaseClassificationResult result, CaseAnalysisOptions? options = null)
    {
        var tokens = _tokenizer.Tokenize(input);
        return _classifier.TryClassify(tokens, out result, options);
    }

    /// <summary>
    /// 入力文字列を解析し、プレフィックス分解情報を含む検査結果を返します。
    /// </summary>
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

    /// <summary>
    /// 入力 Span を解析し、プレフィックス分解情報を含む検査結果を返します。
    /// </summary>
    public SymbolInspectionSpan Inspect(ReadOnlySpan<char> input, CaseAnalysisOptions? options = null)
    {
        var tokens = _tokenizer.Tokenize(input);
        var classification = ClassifySpanInput(input, tokens, options);
        return new SymbolInspectionSpan(input, tokens, classification);
    }

    /// <summary>
    /// 入力文字列を指定スタイルへ変換します。
    /// </summary>
    public string Convert(string input, CaseStyle targetStyle, CaseConversionOptions? options = null)
    {
        var tokens = _tokenizer.Tokenize(input);
        return _converter.Convert(tokens, targetStyle, options);
    }

    /// <summary>
    /// 指定トークン列を指定スタイルへ変換します。
    /// </summary>
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
