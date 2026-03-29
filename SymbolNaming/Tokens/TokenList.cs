using System.Collections;

namespace SymbolNaming.Tokens;

/// <summary>
/// トークン列と任意の source 文字列を保持する読み取り専用リストです。
/// </summary>
public sealed class TokenList : IReadOnlyList<Token>
{
    private readonly List<Token> _tokens;
    private readonly string? _source;

    /// <summary>
    /// source を持たないトークン列を初期化します。
    /// </summary>
    public TokenList(IEnumerable<Token> tokens)
        : this(tokens, null)
    {
    }

    /// <summary>
    /// トークン列を初期化します。
    /// </summary>
    /// <remarks>
    /// <paramref name="source"/> が <see langword="null"/> の場合、<see cref="GetSpan(Token)"/> は使用できません。
    /// </remarks>
    public TokenList(IEnumerable<Token> tokens, string? source)
    {
        _tokens = tokens?.ToList() ?? throw new ArgumentNullException(nameof(tokens));
        _source = source;
    }

    internal TokenList(List<Token> tokens, string? source, bool takeOwnership)
    {
        if (tokens is null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        _tokens = takeOwnership ? tokens : tokens.ToList();
        _source = source;
    }

    /// <summary>
    /// source 文字列を保持しているかを示します。
    /// </summary>
    public bool HasSource => _source is not null;

    /// <summary>
    /// 指定トークンに対応する文字列 Span を取得します。
    /// </summary>
    public ReadOnlySpan<char> GetSpan(Token token)
    {
        if (_source is null)
        {
            throw new InvalidOperationException("Source text is not available for this token list.");
        }

        return token.AsSpan(_source.AsSpan());
    }

    /// <summary>
    /// 指定インデックスのトークンを取得します。
    /// </summary>
    public Token this[int index] => _tokens[index];

    /// <summary>
    /// トークン数を取得します。
    /// </summary>
    public int Count => _tokens.Count;

    /// <summary>
    /// 列挙子を取得します。
    /// </summary>
    public IEnumerator<Token> GetEnumerator()
    {
        return _tokens.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
