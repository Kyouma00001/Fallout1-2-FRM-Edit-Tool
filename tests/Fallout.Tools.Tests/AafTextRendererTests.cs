using Fallout.Tools.Core.AAF;
using Fallout.Tools.Core.Imaging;

namespace Fallout.Tools.Tests;

public sealed class AafTextRendererTests
{
    [Fact]
    public void RenderText_ReturnsExpectedImageSize()
    {
        AafFont font = CreateMinimalFont();
        AafTextRenderer renderer = new(AafRenderPalette.Create(AafPaletteKind.Orange));

        using var image = renderer.RenderText(font, "A A", new AafTextRenderOptions
        {
            Scale = 2,
            LetterSpacing = 1,
            LineSpacing = 0
        });

        Assert.Equal(18, image.Width);
        Assert.Equal(8, image.Height);
    }

    [Fact]
    public void RenderText_SupportsNewLines()
    {
        AafFont font = CreateMinimalFont();
        AafTextRenderer renderer = new(AafRenderPalette.Create(AafPaletteKind.Orange));

        using var image = renderer.RenderText(font, "A\nA", new AafTextRenderOptions
        {
            Scale = 1,
            LetterSpacing = 0,
            LineSpacing = 1
        });

        Assert.Equal(2, image.Width);
        Assert.Equal(9, image.Height);
    }

    [Fact]
    public void RenderText_CanForceUppercaseForAccentedCharacters()
    {
        AafFont font = CreateMinimalFont();
        AafTextRenderer renderer = new(AafRenderPalette.Create(AafPaletteKind.Orange));

        using var lowercaseImage = renderer.RenderText(font, "ç", new AafTextRenderOptions
        {
            Scale = 1,
            ForceUppercase = false
        });

        using var uppercaseImage = renderer.RenderText(font, "ç", new AafTextRenderOptions
        {
            Scale = 1,
            ForceUppercase = true
        });

        Assert.Equal(3, lowercaseImage.Width);
        Assert.Equal(7, uppercaseImage.Width);
    }

    private static AafFont CreateMinimalFont()
    {
        byte[] header = new byte[AafFormat.HeaderSize];
        header[0] = (byte)'A';
        header[1] = (byte)'A';
        header[2] = (byte)'F';
        header[3] = (byte)'F';
        header[4] = 0x00;
        header[5] = 0x04;

        List<AafGlyph> glyphs = new(AafFormat.GlyphCount);
        for (int i = 0; i < AafFormat.GlyphCount; i++)
        {
            if (i == 65)
            {
                glyphs.Add(new AafGlyph(i, 2, 2, 0, AafFormat.BitmapBaseOffset, [1, 0, 1, 1]));
            }
            else if (i == 32)
            {
                glyphs.Add(new AafGlyph(i, 3, 0, 0, AafFormat.BitmapBaseOffset, []));
            }
            else if (i == 199) // Ç
            {
                glyphs.Add(new AafGlyph(i, 7, 2, 0, AafFormat.BitmapBaseOffset, [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]));
            }
            else if (i == 231) // ç
            {
                glyphs.Add(new AafGlyph(i, 3, 2, 0, AafFormat.BitmapBaseOffset, [1, 1, 1, 1, 1, 1]));
            }
            else
            {
                glyphs.Add(new AafGlyph(i, 1, 0, 0, AafFormat.BitmapBaseOffset, []));
            }
        }

        return new AafFont(header, 4, glyphs, "FONTTEST.AAF");
    }
}
