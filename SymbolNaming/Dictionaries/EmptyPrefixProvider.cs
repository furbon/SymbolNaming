namespace SymbolNaming.Dictionaries;

/// <summary>
/// 常に一致しないプレフィックスプロバイダーです。
/// </summary>
public sealed class EmptyPrefixProvider : IPrefixProvider
{
    /// <summary>
    /// 共有インスタンスです。
    /// </summary>
    public static EmptyPrefixProvider Instance { get; } = new();

    private EmptyPrefixProvider()
    {
    }

    /// <summary>
    /// 常に <see langword="false"/> を返します。
    /// </summary>
    public bool IsPrefix(ReadOnlySpan<char> text)
    {
        return false;
    }

    /// <summary>
    /// 常に一致なしとして <paramref name="length"/> を 0 に設定します。
    /// </summary>
    public bool TryMatchLongest(ReadOnlySpan<char> text, int start, out int length)
    {
        length = 0;
        return false;
    }
}
