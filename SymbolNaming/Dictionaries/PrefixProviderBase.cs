namespace SymbolNaming.Dictionaries;

/// <summary>
/// プレフィックスの基本実装を提供する基底クラスです。
/// </summary>
public abstract class PrefixProviderBase : IPrefixProvider
{
    private readonly StringComparer _comparer;
    private readonly object _sync = new();

    private HashSet<string>? _prefixes;
    private string[]? _orderedPrefixes;

    /// <summary>
    /// 指定比較子で初期化します。
    /// </summary>
    protected PrefixProviderBase(StringComparer? comparer = null)
    {
        _comparer = comparer ?? StringComparer.OrdinalIgnoreCase;
    }

    /// <summary>
    /// プレフィックス一覧を返します。
    /// </summary>
    protected virtual IEnumerable<string> GetPrefixes()
    {
        return Array.Empty<string>();
    }

    /// <summary>
    /// キャッシュを無効化します。
    /// </summary>
    protected void InvalidateCache()
    {
        lock (_sync)
        {
            _prefixes = null;
            _orderedPrefixes = null;
        }
    }

    /// <inheritdoc />
    public bool IsPrefix(ReadOnlySpan<char> text)
    {
        EnsureInitialized();
        return _prefixes!.Contains(text.ToString());
    }

    /// <inheritdoc />
    public bool TryMatchLongest(ReadOnlySpan<char> text, int start, out int length)
    {
        if ((uint)start >= (uint)text.Length)
        {
            length = 0;
            return false;
        }

        EnsureInitialized();

        var slice = text.Slice(start);
        var orderedPrefixes = _orderedPrefixes!;

        for (var i = 0; i < orderedPrefixes.Length; ++i)
        {
            var prefix = orderedPrefixes[i];
            if (slice.StartsWith(prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                length = prefix.Length;
                return true;
            }
        }

        length = 0;
        return false;
    }

    private void EnsureInitialized()
    {
        if (_prefixes is not null && _orderedPrefixes is not null)
        {
            return;
        }

        lock (_sync)
        {
            if (_prefixes is not null && _orderedPrefixes is not null)
            {
                return;
            }

            var prefixes = new HashSet<string>(_comparer);
            foreach (var prefix in GetPrefixes())
            {
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }

                prefixes.Add(prefix);
            }

            var orderedPrefixes = prefixes
                .OrderByDescending(static x => x.Length)
                .ThenBy(static x => x, _comparer)
                .ToArray();

            _prefixes = prefixes;
            _orderedPrefixes = orderedPrefixes;
        }
    }
}
