namespace SymbolNaming.Dictionaries;

/// <summary>
/// プレフィックス判定を提供するインターフェイスです。
/// </summary>
public interface IPrefixProvider
{
    /// <summary>
    /// 指定テキストがプレフィックスかどうかを判定します。
    /// </summary>
    bool IsPrefix(ReadOnlySpan<char> text);

    /// <summary>
    /// 指定位置から一致する最長プレフィックス長を取得します。
    /// </summary>
    bool TryMatchLongest(ReadOnlySpan<char> text, int start, out int length);
}
