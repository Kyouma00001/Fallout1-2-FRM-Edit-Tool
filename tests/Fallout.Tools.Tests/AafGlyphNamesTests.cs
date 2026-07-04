using Fallout.Tools.Core.AAF;

namespace Fallout.Tools.Tests;

public sealed class AafGlyphNamesTests
{
    [Theory]
    [InlineData(32, "space")]
    [InlineData(33, "exclamation")]
    [InlineData(34, "quote")]
    [InlineData(65, "A")]
    [InlineData(97, "a")]
    public void GetDisplayName_ReturnsStableNames(int code, string expected)
    {
        Assert.Equal(expected, AafGlyphNames.GetDisplayName(code));
    }
}
