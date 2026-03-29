using SymbolNaming.Analysis;
using SymbolNaming.Conversion;
using SymbolNaming.Lifecycle;
using SymbolNaming.Tokenization;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

/// <summary>
/// Tokenize / Analyze / Convert を統合して提供するエンジンです。
/// </summary>
public sealed class SymbolCaseEngine : IFreezableComponent
{
    private static readonly IInspectionRule[] DefaultInspectionRules =
    {
        new SymbolInspectionWarningAnalyzer(),
    };

    private readonly ISymbolTokenizer _tokenizer;
    private readonly ICaseClassifier _classifier;
    private readonly ICaseConverter _converter;
    private readonly IInspectionRule[] _inspectionRules;
    private readonly object _freezeSync = new();

    private DefaultCaseClassifier? _defaultClassifier;
    private bool _isFrozen;

    /// <summary>
    /// 依存コンポーネントを指定して初期化します。
    /// </summary>
    public SymbolCaseEngine(ISymbolTokenizer tokenizer, ICaseClassifier classifier, ICaseConverter converter, IReadOnlyList<IInspectionRule>? inspectionRules = null)
    {
        _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _inspectionRules = CreateInspectionRules(inspectionRules);

        Freeze();
    }

    /// <summary>
    /// 状態が凍結済みかどうかを取得します。
    /// </summary>
    public bool IsFrozen => _isFrozen;

    /// <summary>
    /// エンジンを凍結し、依存コンポーネントも可能な範囲で凍結します。
    /// </summary>
    public void Freeze()
    {
        if (_isFrozen)
        {
            return;
        }

        lock (_freezeSync)
        {
            if (_isFrozen)
            {
                return;
            }

            if (_tokenizer is IFreezableComponent freezableTokenizer)
            {
                freezableTokenizer.Freeze();
            }

            if (_classifier is IFreezableComponent freezableClassifier)
            {
                freezableClassifier.Freeze();
            }

            if (_converter is IFreezableComponent freezableConverter)
            {
                freezableConverter.Freeze();
            }

            _defaultClassifier = _classifier as DefaultCaseClassifier;
            _isFrozen = true;
        }
    }

    /// <summary>
    /// 文字列をトークン化します。
    /// </summary>
    public TokenList Tokenize(string input)
    {
        EnsureFrozen();
        return _tokenizer.Tokenize(input);
    }

    /// <summary>
    /// 文字列 Span をトークン化します。
    /// </summary>
    public TokenList Tokenize(ReadOnlySpan<char> input)
    {
        EnsureFrozen();
        return _tokenizer.Tokenize(input);
    }

    /// <summary>
    /// 複数の文字列をトークン化します。
    /// </summary>
    public IReadOnlyList<BulkItemResult<TokenList>> TokenizeMany(IReadOnlyList<string> inputs, BulkFailurePolicy failurePolicy = BulkFailurePolicy.FailFast)
    {
        EnsureFrozen();

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return ExecuteMany(inputs.Count, failurePolicy, i => _tokenizer.Tokenize(inputs[i]));
    }

    /// <summary>
    /// 複数の文字列 Memory をトークン化します。
    /// </summary>
    public IReadOnlyList<BulkItemResult<TokenList>> TokenizeMany(IReadOnlyList<ReadOnlyMemory<char>> inputs, BulkFailurePolicy failurePolicy = BulkFailurePolicy.FailFast)
    {
        EnsureFrozen();

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return ExecuteMany(inputs.Count, failurePolicy, i => _tokenizer.Tokenize(inputs[i].Span));
    }

    /// <summary>
    /// 入力文字列の命名スタイルを解析します。
    /// </summary>
    public CaseClassificationResult Analyze(string input, CaseAnalysisOptions? options = null)
    {
        EnsureFrozen();
        var tokens = _tokenizer.Tokenize(input);
        return _classifier.Classify(tokens, options);
    }

    /// <summary>
    /// 入力 Span の命名スタイルを解析します。
    /// </summary>
    public CaseClassificationResult Analyze(ReadOnlySpan<char> input, CaseAnalysisOptions? options = null)
    {
        EnsureFrozen();
        var tokens = _tokenizer.Tokenize(input);
        return ClassifySpanInput(input, tokens, options);
    }

    /// <summary>
    /// 複数の入力文字列を解析します。
    /// </summary>
    public IReadOnlyList<BulkItemResult<CaseClassificationResult>> AnalyzeMany(IReadOnlyList<string> inputs, CaseAnalysisOptions? options = null, BulkFailurePolicy failurePolicy = BulkFailurePolicy.FailFast)
    {
        EnsureFrozen();

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return ExecuteMany(inputs.Count, failurePolicy, i => Analyze(inputs[i], options));
    }

    /// <summary>
    /// 複数の入力 Memory を解析します。
    /// </summary>
    public IReadOnlyList<BulkItemResult<CaseClassificationResult>> AnalyzeMany(IReadOnlyList<ReadOnlyMemory<char>> inputs, CaseAnalysisOptions? options = null, BulkFailurePolicy failurePolicy = BulkFailurePolicy.FailFast)
    {
        EnsureFrozen();

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return ExecuteMany(inputs.Count, failurePolicy, i => Analyze(inputs[i].Span, options));
    }

    /// <summary>
    /// 入力文字列の命名スタイル解析を試行します。
    /// </summary>
    public bool TryAnalyze(string input, out CaseClassificationResult result, CaseAnalysisOptions? options = null)
    {
        EnsureFrozen();
        var tokens = _tokenizer.Tokenize(input);
        return _classifier.TryClassify(tokens, out result, options);
    }

    /// <summary>
    /// 入力 Span の命名スタイル解析を試行します。
    /// </summary>
    public bool TryAnalyze(ReadOnlySpan<char> input, out CaseClassificationResult result, CaseAnalysisOptions? options = null)
    {
        EnsureFrozen();
        var tokens = _tokenizer.Tokenize(input);
        return TryClassifySpanInput(input, tokens, out result, options);
    }

    /// <summary>
    /// 複数の入力文字列について命名スタイル解析を試行します。
    /// </summary>
    public IReadOnlyList<BulkItemResult<BulkTryAnalyzeResult>> TryAnalyzeMany(IReadOnlyList<string> inputs, CaseAnalysisOptions? options = null, BulkFailurePolicy failurePolicy = BulkFailurePolicy.FailFast)
    {
        EnsureFrozen();

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return ExecuteMany(inputs.Count, failurePolicy, i =>
        {
            var success = TryAnalyze(inputs[i], out var result, options);
            return new BulkTryAnalyzeResult(success, result);
        });
    }

    /// <summary>
    /// 複数の入力 Memory について命名スタイル解析を試行します。
    /// </summary>
    public IReadOnlyList<BulkItemResult<BulkTryAnalyzeResult>> TryAnalyzeMany(IReadOnlyList<ReadOnlyMemory<char>> inputs, CaseAnalysisOptions? options = null, BulkFailurePolicy failurePolicy = BulkFailurePolicy.FailFast)
    {
        EnsureFrozen();

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return ExecuteMany(inputs.Count, failurePolicy, i =>
        {
            var success = TryAnalyze(inputs[i].Span, out var result, options);
            return new BulkTryAnalyzeResult(success, result);
        });
    }

    /// <summary>
    /// 入力文字列を解析し、プレフィックス分解情報を含む検査結果を返します。
    /// </summary>
    public SymbolInspection Inspect(string input, CaseAnalysisOptions? options = null)
    {
        EnsureFrozen();

        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var effectiveOptions = options ?? new CaseAnalysisOptions();
        var tokens = _tokenizer.Tokenize(input);
        var classification = _classifier.Classify(tokens, effectiveOptions);
        var warnings = AnalyzeWarnings(input.AsSpan(), tokens, classification);

        CompositeSymbolPatternMatch? compositePattern = null;
        if (effectiveOptions.CompositePatternMatcher.TryMatch(input.AsSpan(), tokens, classification, out var matchedPattern))
        {
            compositePattern = matchedPattern;
        }

        return new SymbolInspection(input, tokens, classification, warnings, compositePattern);
    }

    /// <summary>
    /// 入力 Span を解析し、プレフィックス分解情報を含む検査結果を返します。
    /// </summary>
    public SymbolInspectionSpan Inspect(ReadOnlySpan<char> input, CaseAnalysisOptions? options = null)
    {
        EnsureFrozen();
        var effectiveOptions = options ?? new CaseAnalysisOptions();
        var tokens = _tokenizer.Tokenize(input);
        var classification = ClassifySpanInput(input, tokens, effectiveOptions);
        var warnings = AnalyzeWarnings(input, tokens, classification);

        CompositeSymbolPatternMatch? compositePattern = null;
        if (effectiveOptions.CompositePatternMatcher.TryMatch(input, tokens, classification, out var matchedPattern))
        {
            compositePattern = matchedPattern;
        }

        return new SymbolInspectionSpan(input, tokens, classification, warnings, compositePattern);
    }

    /// <summary>
    /// 複数の入力文字列を検査します。
    /// </summary>
    public IReadOnlyList<BulkItemResult<SymbolInspection>> InspectMany(IReadOnlyList<string> inputs, CaseAnalysisOptions? options = null, BulkFailurePolicy failurePolicy = BulkFailurePolicy.FailFast)
    {
        EnsureFrozen();

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return ExecuteMany(inputs.Count, failurePolicy, i => Inspect(inputs[i], options));
    }

    /// <summary>
    /// 複数の入力 Memory を検査します。
    /// </summary>
    public IReadOnlyList<BulkItemResult<SymbolInspection>> InspectMany(IReadOnlyList<ReadOnlyMemory<char>> inputs, CaseAnalysisOptions? options = null, BulkFailurePolicy failurePolicy = BulkFailurePolicy.FailFast)
    {
        EnsureFrozen();

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return ExecuteMany(inputs.Count, failurePolicy, i => Inspect(inputs[i].ToString(), options));
    }

    /// <summary>
    /// 入力文字列を解析用に正規化し、装飾情報を含む結果を返します。
    /// </summary>
    public SymbolNormalizationResult NormalizeForAnalysis(string input, CaseAnalysisOptions? options = null)
    {
        EnsureFrozen();

        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var tokens = _tokenizer.Tokenize(input);
        var classification = _classifier.Classify(tokens, options);
        return new SymbolNormalizationResult(input, tokens, classification);
    }

    /// <summary>
    /// 入力 Span を解析用に正規化し、装飾情報を含む結果を返します。
    /// </summary>
    public SymbolNormalizationSpan NormalizeForAnalysis(ReadOnlySpan<char> input, CaseAnalysisOptions? options = null)
    {
        EnsureFrozen();
        var tokens = _tokenizer.Tokenize(input);
        var classification = ClassifySpanInput(input, tokens, options);
        return new SymbolNormalizationSpan(input, tokens, classification);
    }

    /// <summary>
    /// 入力文字列を指定スタイルへ変換します。
    /// </summary>
    public CaseConversionResult Convert(string input, CaseStyle targetStyle, CaseConversionOptions? options = null)
    {
        EnsureFrozen();
        var tokens = _tokenizer.Tokenize(input);
        return _converter.Convert(tokens, targetStyle, options);
    }

    /// <summary>
    /// 入力 Span を指定スタイルへ変換します。
    /// </summary>
    public CaseConversionResult Convert(ReadOnlySpan<char> input, CaseStyle targetStyle, CaseConversionOptions? options = null)
    {
        EnsureFrozen();
        var sourceText = input.ToString();
        var tokens = _tokenizer.Tokenize(sourceText);
        return _converter.Convert(tokens, targetStyle, options);
    }

    /// <summary>
    /// 指定トークン列を指定スタイルへ変換します。
    /// </summary>
    public CaseConversionResult Convert(TokenList tokens, CaseStyle targetStyle, CaseConversionOptions? options = null)
    {
        EnsureFrozen();
        return _converter.Convert(tokens, targetStyle, options);
    }

    /// <summary>
    /// 複数の入力文字列を指定スタイルへ変換します。
    /// </summary>
    public IReadOnlyList<BulkItemResult<CaseConversionResult>> ConvertMany(IReadOnlyList<string> inputs, CaseStyle targetStyle, CaseConversionOptions? options = null, BulkFailurePolicy failurePolicy = BulkFailurePolicy.FailFast)
    {
        EnsureFrozen();

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return ExecuteMany(inputs.Count, failurePolicy, i => Convert(inputs[i], targetStyle, options));
    }

    /// <summary>
    /// 複数の入力 Memory を指定スタイルへ変換します。
    /// </summary>
    public IReadOnlyList<BulkItemResult<CaseConversionResult>> ConvertMany(IReadOnlyList<ReadOnlyMemory<char>> inputs, CaseStyle targetStyle, CaseConversionOptions? options = null, BulkFailurePolicy failurePolicy = BulkFailurePolicy.FailFast)
    {
        EnsureFrozen();

        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        return ExecuteMany(inputs.Count, failurePolicy, i => Convert(inputs[i].Span, targetStyle, options));
    }

    private CaseClassificationResult ClassifySpanInput(ReadOnlySpan<char> input, TokenList tokens, CaseAnalysisOptions? options)
    {
        if (_defaultClassifier is not null)
        {
            return _defaultClassifier.Classify(input, tokens, options);
        }

        var sourceText = input.ToString();
        var sourceTokens = _tokenizer.Tokenize(sourceText);
        return _classifier.Classify(sourceTokens, options);
    }

    private bool TryClassifySpanInput(ReadOnlySpan<char> input, TokenList tokens, out CaseClassificationResult result, CaseAnalysisOptions? options)
    {
        if (_defaultClassifier is not null)
        {
            return _defaultClassifier.TryClassify(input, tokens, out result, options);
        }

        var sourceText = input.ToString();
        var sourceTokens = _tokenizer.Tokenize(sourceText);
        return _classifier.TryClassify(sourceTokens, out result, options);
    }

    private void EnsureFrozen()
    {
        if (!_isFrozen)
        {
            throw new InvalidOperationException("Engine is not frozen. Call Freeze() before using it.");
        }
    }

    private static IInspectionRule[] CreateInspectionRules(IReadOnlyList<IInspectionRule>? inspectionRules)
    {
        if (inspectionRules is null)
        {
            return DefaultInspectionRules;
        }

        if (inspectionRules.Count == 0)
        {
            return Array.Empty<IInspectionRule>();
        }

        var copied = new IInspectionRule[inspectionRules.Count];
        for (var i = 0; i < inspectionRules.Count; i++)
        {
            copied[i] = inspectionRules[i] ?? throw new ArgumentException("Inspection rule cannot be null.", nameof(inspectionRules));
        }

        return copied;
    }

    private IReadOnlyList<SymbolInspectionWarning> AnalyzeWarnings(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification)
    {
        if (_inspectionRules.Length == 0)
        {
            return Array.Empty<SymbolInspectionWarning>();
        }

        List<SymbolInspectionWarning>? warnings = null;

        for (var i = 0; i < _inspectionRules.Length; i++)
        {
            if (!_inspectionRules[i].TryCreateWarning(source, tokens, classification, out var warning))
            {
                continue;
            }

            warnings ??= new List<SymbolInspectionWarning>(_inspectionRules.Length);
            warnings.Add(warning);
        }

        return warnings ?? (IReadOnlyList<SymbolInspectionWarning>)Array.Empty<SymbolInspectionWarning>();
    }

    private static IReadOnlyList<BulkItemResult<T>> ExecuteMany<T>(int count, BulkFailurePolicy failurePolicy, Func<int, T> action)
    {
        var results = new BulkItemResult<T>[count];

        for (var i = 0; i < count; i++)
        {
            try
            {
                results[i] = BulkItemResult<T>.Success(i, action(i));
            }
            catch (Exception ex)
            {
                if (failurePolicy == BulkFailurePolicy.FailFast)
                {
                    throw;
                }

                results[i] = BulkItemResult<T>.Failure(i, ex);
            }
        }

        return results;
    }
}
