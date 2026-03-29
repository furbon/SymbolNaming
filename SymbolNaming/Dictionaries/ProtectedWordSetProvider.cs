namespace SymbolNaming.Dictionaries;

/// <summary>
/// 単語集合を使う保護語プロバイダーです。
/// </summary>
public sealed class ProtectedWordSetProvider : ProtectedWordProviderBase
{
    private readonly string[] _words;

    /// <summary>
    /// 指定単語群で初期化します。
    /// </summary>
    public ProtectedWordSetProvider(IEnumerable<string> words, StringComparer? comparer = null)
        : base(comparer)
    {
        if (words is null)
        {
            throw new ArgumentNullException(nameof(words));
        }

        _words = words.ToArray();
    }

    /// <summary>
    /// 指定単語群で初期化します。
    /// </summary>
    public ProtectedWordSetProvider(params string[] words)
        : this((IEnumerable<string>)words)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<string> GetProtectedWords()
    {
        return _words;
    }
}
