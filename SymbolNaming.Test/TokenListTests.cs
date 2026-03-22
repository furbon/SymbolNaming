using SymbolNaming.Tokens;

namespace SymbolNaming.Test;

public class TokenListTests
{
    [Fact]
    public void tokensがnullならArgumentNullExceptionを送出する()
    {
        Assert.Throws<ArgumentNullException>(() => new TokenList(null!));
    }

    [Fact]
    public void sourceなしでGetSpanするとInvalidOperationExceptionを送出する()
    {
        var token = new Token(0, 4, TokenCategory.Word);
        var tokenList = new TokenList(new[] { token });

        Assert.Throws<InvalidOperationException>(() => tokenList.GetSpan(token));
    }
}
