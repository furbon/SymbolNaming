using SymbolNaming.Analysis;
using SymbolNaming.Tokens;

namespace SymbolNaming.Test;

public class CompositeSuffixPatternMatcherTests
{
    [Fact]
    public void ルールIDが重複する場合は構築時にArgumentExceptionを送出する()
    {
        Assert.Throws<ArgumentException>(() =>
            new CompositeSuffixPatternMatcher(
                new TestCompositeRule("SameId", true),
                new TestCompositeRule("SameId", false)));
    }

    [Fact]
    public void ルールIDが空白のみの場合は構築時にArgumentExceptionを送出する()
    {
        Assert.Throws<ArgumentException>(() =>
            new CompositeSuffixPatternMatcher(
                new TestCompositeRule(" ", true)));
    }

    [Fact]
    public void TryMatchは可変サフィックス要件を維持して一致判定できる()
    {
        var matcher = new CompositeSuffixPatternMatcher(
            new RegexCompositeSuffixPatternRule(
                "UpperTagOrSegments",
                "^[A-Z]+(?:_[A-Z0-9]+)*$",
                "^[A-Z][A-Za-z0-9]*$"),
            new RegexCompositeSuffixPatternRule(
                "PascalOrAlphaNumSegments",
                "^[A-Z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)*$",
                "^[A-Z][A-Za-z0-9]*$"));

        var source = "EnemyUserData_WALK_SP".AsSpan();
        var tokens = new TokenList(new[]
        {
            new Token(0, 5, TokenCategory.Word),
            new Token(5, 4, TokenCategory.Word),
            new Token(9, 4, TokenCategory.Word),
            new Token(13, 1, TokenCategory.Separator),
            new Token(14, 4, TokenCategory.Word),
            new Token(18, 1, TokenCategory.Separator),
            new Token(19, 2, TokenCategory.Word),
        });

        var matched = matcher.TryMatch(source, tokens, CaseClassificationResult.Unknown, out var match);

        Assert.True(matched);
        Assert.Equal("UpperTagOrSegments", match.PatternId);
        Assert.Equal("EnemyUserData", source.Slice(match.BaseStart, match.BaseLength).ToString());
        Assert.Equal("WALK_SP", source.Slice(match.SuffixStart, match.SuffixLength).ToString());
    }

    [Fact]
    public void TryMatchはPrefix付き入力でも一致判定できる()
    {
        var matcher = new CompositeSuffixPatternMatcher(
            new RegexCompositeSuffixPatternRule(
                "UpperTagOrSegments",
                "^[A-Z]+(?:_[A-Z0-9]+)*$",
                "^[A-Z][A-Za-z0-9]*$"));

        var source = "s_EnemyUserData_WALK_NORMAL".AsSpan();
        var tokens = new TokenList(new[]
        {
            new Token(0, 1, TokenCategory.Prefix),
            new Token(1, 1, TokenCategory.Separator),
            new Token(2, 5, TokenCategory.Word),
            new Token(7, 4, TokenCategory.Word),
            new Token(11, 4, TokenCategory.Word),
            new Token(15, 1, TokenCategory.Separator),
            new Token(16, 4, TokenCategory.Word),
            new Token(20, 1, TokenCategory.Separator),
            new Token(21, 6, TokenCategory.Word),
        });

        var classification = new CaseClassificationResult(CaseStyle.Unknown, prefixed: true);
        var matched = matcher.TryMatch(source, tokens, classification, out var match);

        Assert.True(matched);
        Assert.Equal("EnemyUserData", source.Slice(match.BaseStart, match.BaseLength).ToString());
        Assert.Equal("WALK_NORMAL", source.Slice(match.SuffixStart, match.SuffixLength).ToString());
    }

    private sealed class TestCompositeRule : ICompositeSuffixPatternRule
    {
        private readonly bool _isMatch;

        public TestCompositeRule(string patternId, bool isMatch)
        {
            PatternId = patternId;
            _isMatch = isMatch;
        }

        public string PatternId { get; }

        public bool IsMatch(ReadOnlySpan<char> baseName, ReadOnlySpan<char> suffix)
        {
            return _isMatch;
        }
    }
}
