using SymbolNaming.Tokens;

namespace SymbolNaming.Tokenization.SplitRules;

public readonly ref struct SplitResult
{
    public readonly bool IsSplit;
    public readonly int ConsumeCount;
    public readonly TokenCategory Category;

    public bool Consumed => ConsumeCount > 0;

    private SplitResult(bool isSplit, int consumeCount, TokenCategory category)
    {
        IsSplit = isSplit;
        ConsumeCount = consumeCount;
        Category = category;
    }

    public static SplitResult NoSplit => new(false, 0, default);

    public static SplitResult WordSplit(TokenCategory category = TokenCategory.Word) => new(true, 0, category);

    public static SplitResult SeparatorSplit(int consumeCount = 1, TokenCategory category = TokenCategory.Separator) => new(true, consumeCount, category);
}
