using SymbolNaming.Analysis;

namespace SymbolNaming.Engine;

/// <summary>
/// バルク TryAnalyze の 1 要素分の結果を保持します。
/// </summary>
public readonly struct BulkTryAnalyzeResult
{
    /// <summary>
    /// 解析結果を初期化します。
    /// </summary>
    public BulkTryAnalyzeResult(bool success, CaseClassificationResult result)
    {
        Success = success;
        Result = result;
    }

    /// <summary>
    /// TryAnalyze の成否です。
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// 解析結果です。
    /// </summary>
    public CaseClassificationResult Result { get; }
}
