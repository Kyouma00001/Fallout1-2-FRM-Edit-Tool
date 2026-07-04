using Fallout.Tools.Core.AAF;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Fallout.Tools.Core.Imaging;

public sealed class AafGlyphRenderer
{
    private readonly AafRenderPalette _palette;

    public AafGlyphRenderer(AafRenderPalette palette)
    {
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
    }

    public Image<Rgba32> RenderGlyph(AafFont font, AafGlyph glyph, int scale = 1)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(glyph);

        if (scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be greater than zero.");
        }

        int logicalWidth = Math.Max(1, (int)glyph.Width);
        int logicalHeight = Math.Max(1, (int)font.MaxHeight);

        Image<Rgba32> image = new Image<Rgba32>(logicalWidth * scale, logicalHeight * scale);

        if (!glyph.HasBitmap)
        {
            return image;
        }

        int yOffset = Math.Max(0, (int)font.MaxHeight - glyph.Height);

        for (int y = 0; y < glyph.Height; y++)
        {
            for (int x = 0; x < glyph.Width; x++)
            {
                byte value = glyph.Pixels[y * glyph.Width + x];
                if (value == 0) continue;

                Rgba32 color = _palette.GetColor(value);
                DrawScaledPixel(image, x, y + yOffset, scale, color);
            }
        }

        return image;
    }

    public Image<Rgba32> RenderAtlas(AafFont font, int scale = 1, int columns = 16, int cellPadding = 1)
    {
        ArgumentNullException.ThrowIfNull(font);

        if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
        if (cellPadding < 0) throw new ArgumentOutOfRangeException(nameof(cellPadding));

        int rows = (int)Math.Ceiling(font.Glyphs.Count / (double)columns);
        int maxGlyphWidth = Math.Max(1, font.MaxGlyphWidth);
        int maxGlyphHeight = Math.Max(1, (int)font.MaxHeight);
        int cellWidth = maxGlyphWidth * scale + cellPadding * 2;
        int cellHeight = maxGlyphHeight * scale + cellPadding * 2;

        Image<Rgba32> atlas = new Image<Rgba32>(columns * cellWidth, rows * cellHeight);

        for (int i = 0; i < font.Glyphs.Count; i++)
        {
            AafGlyph glyph = font.Glyphs[i];
            int cellX = (i % columns) * cellWidth + cellPadding;
            int cellY = (i / columns) * cellHeight + cellPadding;

            if (!glyph.HasBitmap) continue;

            int yOffset = Math.Max(0, (int)font.MaxHeight - glyph.Height);
            for (int y = 0; y < glyph.Height; y++)
            {
                for (int x = 0; x < glyph.Width; x++)
                {
                    byte value = glyph.Pixels[y * glyph.Width + x];
                    if (value == 0) continue;

                    Rgba32 color = _palette.GetColor(value);
                    DrawScaledPixel(atlas, x, y + yOffset, scale, color, cellX, cellY);
                }
            }
        }

        return atlas;
    }

    private static void DrawScaledPixel(Image<Rgba32> image, int logicalX, int logicalY, int scale, Rgba32 color)
    {
        int startX = logicalX * scale;
        int startY = logicalY * scale;

        for (int yy = 0; yy < scale; yy++)
        {
            for (int xx = 0; xx < scale; xx++)
            {
                image[startX + xx, startY + yy] = color;
            }
        }
    }

    private static void DrawScaledPixel(Image<Rgba32> image, int logicalX, int logicalY, int scale, Rgba32 color, int baseX, int baseY)
    {
        int startX = baseX + logicalX * scale;
        int startY = baseY + logicalY * scale;

        for (int yy = 0; yy < scale; yy++)
        {
            for (int xx = 0; xx < scale; xx++)
            {
                image[startX + xx, startY + yy] = color;
            }
        }
    }
}
