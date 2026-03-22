namespace SymbolNaming.Dictionaries;

/// <summary>
/// 保護語（辞書語）判定を提供するインターフェイスです。
/// </summary>
public interface IProtectedWordProvider
{
    /// <summary>
    /// 指定テキストが保護語かどうかを判定します。
    /// </summary>
    bool IsProtected(ReadOnlySpan<char> text);

    /// <summary>
    /// 指定位置から一致する最長保護語長を取得します。
    /// </summary>
    bool TryMatchLongest(ReadOnlySpan<char> text, int start, out int length);
}
