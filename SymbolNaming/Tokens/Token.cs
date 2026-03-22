namespace SymbolNaming.Tokens;

/// <summary>
/// シンボル文字列内のトークン位置情報を表します。
/// </summary>
public readonly struct Token
{
    /// <summary>
    /// 元文字列内での開始位置です。
    /// </summary>
    public readonly int Start;

    /// <summary>
    /// トークン長です。
    /// </summary>
    public readonly int Length;

    /// <summary>
    /// トークンのカテゴリです。
    /// </summary>
    public readonly TokenCategory Category;

    /// <summary>
    /// 新しいトークンを初期化します。
    /// </summary>
    public Token(int start, int length, TokenCategory category)
    {
        Start = start;
        Length = length;
        Category = category;
    }

    /// <summary>
    /// 指定入力からこのトークン範囲の Span を取得します。
    /// </summary>
    public ReadOnlySpan<char> AsSpan(ReadOnlySpan<char> input)
    {
        return input.Slice(Start, Length);
    }
}
