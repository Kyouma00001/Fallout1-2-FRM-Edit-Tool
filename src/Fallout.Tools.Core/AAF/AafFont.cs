namespace Fallout.Tools.Core.AAF;

public sealed class AafFont
{
    public AafFont(byte[] header, ushort maxHeight, IReadOnlyList<AafGlyph> glyphs, string? sourcePath = null)
    {
        if (header is null) throw new ArgumentNullException(nameof(header));
        if (header.Length != AafFormat.HeaderSize)
        {
            throw new ArgumentException($"AAF header must be {AafFormat.HeaderSize} bytes.", nameof(header));
        }

        if (glyphs is null) throw new ArgumentNullException(nameof(glyphs));
        if (glyphs.Count != AafFormat.GlyphCount)
        {
            throw new ArgumentException($"AAF fonts must contain {AafFormat.GlyphCount} glyphs.", nameof(glyphs));
        }

        Header = header;
        MaxHeight = maxHeight;
        Glyphs = glyphs;
        SourcePath = sourcePath;
    }

    public byte[] Header { get; }

    public ushort MaxHeight { get; }

    public IReadOnlyList<AafGlyph> Glyphs { get; }

    public string? SourcePath { get; }

    public int NonEmptyGlyphCount => Glyphs.Count(g => g.HasBitmap);

    public int AdvanceOnlyGlyphCount => Glyphs.Count(g => g.HasAdvanceOnly);

    public int MaxGlyphWidth => Glyphs.Count == 0 ? 0 : Glyphs.Max(g => g.Width);

    public long BitmapBytes => Glyphs.Sum(g => (long)g.Pixels.Length);
}
