using Fallout.Tools.Core.AAF;
using Fallout.Tools.Core.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Fallout.Tools.Tests;

public sealed class UiTextComposerTests
{
    [Fact]
    public void Compose_draws_text_pixels_on_background()
    {
        AafFont font = CreateTestFont();
        AafTextRenderer renderer = new AafTextRenderer(AafRenderPalette.Create(AafPaletteKind.Orange));
        UiTextComposer composer = new UiTextComposer(renderer);

        using Image<Rgba32> background = new Image<Rgba32>(20, 10);
        UiTextPlacement placement = new("TEST", 2, 3, 0, UiTextAlignment.Left, "A");

        using Image<Rgba32> composed = composer.Compose(background, font, new[] { placement });

        Assert.NotEqual(default, composed[2, 3]);
        Assert.Equal(default, composed[0, 0]);
    }

    private static AafFont CreateTestFont()
    {
        byte[] header = new byte[AafFormat.HeaderSize];
        List<AafGlyph> glyphs = new(capacity: AafFormat.GlyphCount);

        for (int i = 0; i < AafFormat.GlyphCount; i++)
        {
            glyphs.Add(new AafGlyph(i, 1, 0, 0, 0, Array.Empty<byte>()));
        }

        glyphs['A'] = new AafGlyph('A', 1, 1, 0, 0, new byte[] { 9 });

        return new AafFont(header, maxHeight: 1, glyphs);
    }
}
