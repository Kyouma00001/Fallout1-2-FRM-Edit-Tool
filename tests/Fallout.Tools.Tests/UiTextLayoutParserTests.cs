using Fallout.Tools.Core.Imaging;

namespace Fallout.Tools.Tests;

public sealed class UiTextLayoutParserTests
{
    [Fact]
    public void ParseLines_supports_simple_layout_lines()
    {
        IReadOnlyList<UiTextPlacement> placements = UiTextLayoutParser.ParseLines(new[]
        {
            "# comment",
            "BARTER|10|20|negociar"
        });

        Assert.Single(placements);
        Assert.Equal("BARTER", placements[0].Name);
        Assert.Equal(10, placements[0].X);
        Assert.Equal(20, placements[0].Y);
        Assert.Equal(0, placements[0].Width);
        Assert.Equal(UiTextAlignment.Left, placements[0].Alignment);
        Assert.Equal("negociar", placements[0].Text);
    }

    [Fact]
    public void ParseLines_supports_aligned_layout_lines()
    {
        IReadOnlyList<UiTextPlacement> placements = UiTextLayoutParser.ParseLines(new[]
        {
            "DONE|5|6|80|center|pronto"
        });

        Assert.Single(placements);
        Assert.Equal("DONE", placements[0].Name);
        Assert.Equal(5, placements[0].X);
        Assert.Equal(6, placements[0].Y);
        Assert.Equal(80, placements[0].Width);
        Assert.Equal(UiTextAlignment.Center, placements[0].Alignment);
        Assert.Equal("pronto", placements[0].Text);
    }
}
