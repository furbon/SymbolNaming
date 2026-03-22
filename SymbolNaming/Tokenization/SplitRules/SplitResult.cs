using SymbolNaming.Tokens;

namespace SymbolNaming.Tokenization.SplitRules;

/// <summary>
/// 分割判定の結果を表します。
/// </summary>
public readonly ref struct SplitResult
{
    /// <summary>
    /// 分割判定が成立したかどうかを示します。
    /// </summary>
    public readonly bool IsSplit;

    /// <summary>
    /// 区切りトークンとして消費する文字数です。
    /// </summary>
    public readonly int ConsumeCount;

    /// <summary>
    /// 生成されるトークンカテゴリです。
    /// </summary>
    public readonly TokenCategory Category;

    /// <summary>
    /// 文字消費を伴う分割かどうかを示します。
    /// </summary>
    public bool Consumed => ConsumeCount > 0;

    private SplitResult(bool isSplit, int consumeCount, TokenCategory category)
    {
        IsSplit = isSplit;
        ConsumeCount = consumeCount;
        Category = category;
    }

    /// <summary>
    /// 分割なしを表す結果です。
    /// </summary>
    public static SplitResult NoSplit => new(false, 0, default);

    /// <summary>
    /// 文字消費なしの語分割結果を生成します。
    /// </summary>
    public static SplitResult WordSplit(TokenCategory category = TokenCategory.Word) => new(true, 0, category);

    /// <summary>
    /// 区切り文字を消費する分割結果を生成します。
    /// </summary>
    public static SplitResult SeparatorSplit(int consumeCount = 1, TokenCategory category = TokenCategory.Separator) => new(true, consumeCount, category);
}
