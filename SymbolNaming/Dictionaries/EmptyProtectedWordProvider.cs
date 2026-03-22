namespace SymbolNaming.Dictionaries;

/// <summary>
/// 常に一致しない保護語プロバイダーです。
/// </summary>
public sealed class EmptyProtectedWordProvider : IProtectedWordProvider
{
    /// <summary>
    /// 共有インスタンスです。
    /// </summary>
    public static EmptyProtectedWordProvider Instance { get; } = new();

    private EmptyProtectedWordProvider()
    {
    }

    /// <summary>
    /// 常に <see langword="false"/> を返します。
    /// </summary>
    public bool IsProtected(ReadOnlySpan<char> text)
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
