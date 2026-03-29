namespace SymbolNaming.Engine;

/// <summary>
/// バルク処理時の失敗時ポリシーです。
/// </summary>
public enum BulkFailurePolicy
{
    /// <summary>
    /// 先頭の失敗で例外を送出して処理を中断します。
    /// </summary>
    FailFast = 0,

    /// <summary>
    /// 失敗を結果に保持して処理を継続します。
    /// </summary>
    CollectErrors = 1,
}
