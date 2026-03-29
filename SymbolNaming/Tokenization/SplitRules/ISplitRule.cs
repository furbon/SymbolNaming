namespace SymbolNaming.Tokenization.SplitRules;

/// <summary>
/// 分割ポイント判定ルールを表すインターフェイスです。
/// </summary>
public interface ISplitRule
{
    /// <summary>
    /// 指定位置で分割するかどうかを判定します。
    /// </summary>
    SplitResult Check(ReadOnlySpan<char> span, int index);
}
