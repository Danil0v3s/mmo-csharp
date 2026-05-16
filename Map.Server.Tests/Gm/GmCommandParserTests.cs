using Map.Server.Gm;

namespace Map.Server.Tests.Gm;

public class GmCommandParserTests
{
    [Fact]
    public void TryParse_PlainChat_ReturnsFalse()
    {
        Assert.False(GmCommandParser.TryParse("Hero : hello there", out _, out _));
    }

    [Fact]
    public void TryParse_AtSymbol_StripsAndLowercasesName()
    {
        Assert.True(GmCommandParser.TryParse("Hero : @KillMob", out var name, out var args));
        Assert.Equal("killmob", name);
        Assert.Empty(args);
    }

    [Fact]
    public void TryParse_TokenizesArgs()
    {
        Assert.True(GmCommandParser.TryParse("Hero : @warp 156 191", out var name, out var args));
        Assert.Equal("warp", name);
        Assert.Equal(new[] { "156", "191" }, args);
    }

    [Fact]
    public void TryParse_AcceptsPoundSymbol()
    {
        Assert.True(GmCommandParser.TryParse("Hero : #help target", out var name, out var args));
        Assert.Equal("help", name);
        Assert.Equal(new[] { "target" }, args);
    }

    [Fact]
    public void TryParse_SquashesExtraSpaces()
    {
        Assert.True(GmCommandParser.TryParse("Hero : @warp    156   191", out _, out var args));
        Assert.Equal(new[] { "156", "191" }, args);
    }

    [Fact]
    public void TryParse_TolerantOfNoNamePrefix()
    {
        // Some packet shapes omit the "<name> : " prefix; the parser should
        // still pick up the @-command.
        Assert.True(GmCommandParser.TryParse("@where", out var name, out _));
        Assert.Equal("where", name);
    }

    [Fact]
    public void TryParse_EmptyStringReturnsFalse()
    {
        Assert.False(GmCommandParser.TryParse(string.Empty, out _, out _));
        Assert.False(GmCommandParser.TryParse("Hero : @", out _, out _));
    }
}
