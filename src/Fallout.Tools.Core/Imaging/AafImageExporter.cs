using Fallout.Tools.Core.AAF;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Fallout.Tools.Core.Imaging;

public sealed class AafImageExporter
{
    private readonly AafGlyphRenderer _renderer;

    public AafImageExporter(AafGlyphRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public void ExportGlyphs(AafFont font, string outputDirectory, int scale = 1, bool exportAtlas = true)
    {
        ArgumentNullException.ThrowIfNull(font);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
        }

        Directory.CreateDirectory(outputDirectory);

        foreach (AafGlyph glyph in font.Glyphs)
        {
            using Image<Rgba32> image = _renderer.RenderGlyph(font, glyph, scale);
            string filePath = Path.Combine(outputDirectory, AafGlyphNames.GetFileName(glyph.Index));
            image.SaveAsPng(filePath);
        }

        if (exportAtlas)
        {
            using Image<Rgba32> atlas = _renderer.RenderAtlas(font, scale);
            atlas.SaveAsPng(Path.Combine(outputDirectory, "atlas.png"));
        }
    }
}
