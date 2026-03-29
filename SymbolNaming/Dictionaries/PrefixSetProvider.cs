namespace SymbolNaming.Dictionaries;

/// <summary>
/// 文字列集合を使うプレフィックスプロバイダーです。
/// </summary>
public sealed class PrefixSetProvider : PrefixProviderBase
{
    private readonly string[] _prefixes;

    /// <summary>
    /// 指定プレフィックス群で初期化します。
    /// </summary>
    public PrefixSetProvider(IEnumerable<string> prefixes, StringComparer? comparer = null)
        : base(comparer)
    {
        if (prefixes is null)
        {
            throw new ArgumentNullException(nameof(prefixes));
        }

        _prefixes = prefixes.ToArray();
    }

    /// <summary>
    /// 指定プレフィックス群で初期化します。
    /// </summary>
    public PrefixSetProvider(params string[] prefixes)
        : this((IEnumerable<string>)prefixes)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<string> GetPrefixes()
    {
        return _prefixes;
    }
}
