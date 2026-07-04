using Fallout.Tools.Core.AAF;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Fallout.Tools.Core.Imaging;

public sealed class AafTextRenderer
{
    private readonly AafRenderPalette _palette;

    public AafTextRenderer(AafRenderPalette palette)
    {
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
    }

    public Image<Rgba32> RenderText(AafFont font, string text, AafTextRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        options ??= AafTextRenderOptions.Default;

        if (options.Scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Scale must be greater than zero.");
        }

        if (options.LetterSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "LetterSpacing cannot be negative.");
        }

        if (options.LineSpacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "LineSpacing cannot be negative.");
        }

        string renderText = options.ForceUppercase
            ? (text ?? string.Empty).ToUpperInvariant()
            : text ?? string.Empty;

        string[] lines = NormalizeLines(renderText);

        int fontHeight = Math.Max(1, (int)font.MaxHeight);
        int logicalWidth = Math.Max(1, lines.Max(line => MeasureLine(font, line, options.LetterSpacing)));
        int logicalHeight = Math.Max(1, lines.Length * fontHeight + Math.Max(0, lines.Length - 1) * options.LineSpacing);

        Image<Rgba32> image = new Image<Rgba32>(logicalWidth * options.Scale, logicalHeight * options.Scale);

        int y = 0;

        foreach (string line in lines)
        {
            DrawLine(image, font, line, 0, y, options);
            y += fontHeight + options.LineSpacing;
        }

        return image;
    }

    private static string[] NormalizeLines(string text)
    {
        string normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        return normalized.Split('\n');
    }

    private static int MeasureLine(AafFont font, string line, int letterSpacing)
    {
        if (line.Length == 0)
        {
            return 1;
        }

        int width = 0;

        for (int i = 0; i < line.Length; i++)
        {
            AafGlyph glyph = GetGlyph(font, line[i]);

            width += Math.Max(1, (int)glyph.Width);

            if (i + 1 < line.Length)
            {
                width += letterSpacing;
            }
        }

        return Math.Max(1, width);
    }

    private void DrawLine(Image<Rgba32> image, AafFont font, string line, int startX, int startY, AafTextRenderOptions options)
    {
        int x = startX;

        for (int i = 0; i < line.Length; i++)
        {
            AafGlyph glyph = GetGlyph(font, line[i]);

            DrawGlyph(image, font, glyph, x, startY, options.Scale);

            x += Math.Max(1, (int)glyph.Width);

            if (i + 1 < line.Length)
            {
                x += options.LetterSpacing;
            }
        }
    }

    private void DrawGlyph(Image<Rgba32> image, AafFont font, AafGlyph glyph, int logicalX, int logicalY, int scale)
    {
        if (!glyph.HasBitmap)
        {
            return;
        }

        int yOffset = Math.Max(0, (int)font.MaxHeight - (int)glyph.Height);

        for (int y = 0; y < glyph.Height; y++)
        {
            for (int x = 0; x < glyph.Width; x++)
            {
                byte value = glyph.Pixels[y * glyph.Width + x];

                if (value == 0)
                {
                    continue;
                }

                Rgba32 color = _palette.GetColor(value);
                DrawScaledPixel(image, logicalX + x, logicalY + y + yOffset, scale, color);
            }
        }
    }

    private static AafGlyph GetGlyph(AafFont font, char c)
    {
        int index = c <= byte.MaxValue ? c : '?';
        return font.Glyphs[index];
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
}

public sealed class AafTextRenderOptions
{
    public int Scale { get; init; } = 1;

    public int LetterSpacing { get; init; } = 0;

    public int LineSpacing { get; init; } = 0;

    public bool ForceUppercase { get; init; }

    public static AafTextRenderOptions Default { get; } = new();
}
