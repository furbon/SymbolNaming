namespace SymbolNaming.Dictionaries;

/// <summary>
/// 保護語の基本実装を提供する基底クラスです。
/// </summary>
public abstract class ProtectedWordProviderBase : IProtectedWordProvider
{
    private readonly StringComparer _comparer;
    private readonly object _sync = new();

    private HashSet<string>? _words;
    private string[]? _orderedWords;

    /// <summary>
    /// 指定比較子で初期化します。
    /// </summary>
    protected ProtectedWordProviderBase(StringComparer? comparer = null)
    {
        _comparer = comparer ?? StringComparer.OrdinalIgnoreCase;
    }

    /// <summary>
    /// 保護語一覧を返します。
    /// </summary>
    protected virtual IEnumerable<string> GetProtectedWords()
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
            _words = null;
            _orderedWords = null;
        }
    }

    /// <inheritdoc />
    public bool IsProtected(ReadOnlySpan<char> text)
    {
        EnsureInitialized();
        return _words!.Contains(text.ToString());
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
        var orderedWords = _orderedWords!;

        for (var i = 0; i < orderedWords.Length; ++i)
        {
            var word = orderedWords[i];
            if (slice.StartsWith(word.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                length = word.Length;
                return true;
            }
        }

        length = 0;
        return false;
    }

    private void EnsureInitialized()
    {
        if (_words is not null && _orderedWords is not null)
        {
            return;
        }

        lock (_sync)
        {
            if (_words is not null && _orderedWords is not null)
            {
                return;
            }

            var words = new HashSet<string>(_comparer);
            foreach (var word in GetProtectedWords())
            {
                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                words.Add(word);
            }

            var orderedWords = words
                .OrderByDescending(static x => x.Length)
                .ThenBy(static x => x, _comparer)
                .ToArray();

            _words = words;
            _orderedWords = orderedWords;
        }
    }
}
