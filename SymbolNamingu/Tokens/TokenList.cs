using System.Collections;

namespace SymbolNaming.Tokens;

public sealed class TokenList : IReadOnlyList<Token>
{
    private readonly List<Token> _tokens;
    private readonly string? _source;

    public TokenList(IEnumerable<Token> tokens)
        : this(tokens, null)
    {
    }

    public TokenList(IEnumerable<Token> tokens, string? source)
    {
        _tokens = tokens?.ToList() ?? throw new ArgumentNullException(nameof(tokens));
        _source = source;
    }

    public bool HasSource => _source is not null;

    public ReadOnlySpan<char> GetSpan(Token token)
    {
        if (_source is null)
        {
            throw new InvalidOperationException("Source text is not available for this token list.");
        }

        return token.AsSpan(_source.AsSpan());
    }

    public Token this[int index] => _tokens[index];

    public int Count => _tokens.Count;

    public IEnumerator<Token> GetEnumerator()
    {
        return _tokens.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
