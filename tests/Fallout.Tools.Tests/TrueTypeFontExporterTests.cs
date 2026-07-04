using Fallout.Tools.Core.AAF;
using Fallout.Tools.Core.Fonts;

namespace Fallout.Tools.Tests;

public sealed class TrueTypeFontExporterTests
{
    [Fact]
    public void Build_MinimalAaf_ReturnsTrueTypeSfnt()
    {
        AafFont font = CreateMinimalFont();

        byte[] ttf = new TrueTypeFontExporter().Build(font, new TrueTypeExportOptions
        {
            FamilyName = "TestFont",
            UnitsPerPixel = 64
        });

        Assert.True(ttf.Length > 12);
        Assert.Equal(new byte[] { 0x00, 0x01, 0x00, 0x00 }, ttf.Take(4).ToArray());
        Assert.Contains(SplitIntoWords(ttf), word => word.SequenceEqual(EncodingBytes("glyf")));
        Assert.Contains(SplitIntoWords(ttf), word => word.SequenceEqual(EncodingBytes("cmap")));
        Assert.Contains(SplitIntoWords(ttf), word => word.SequenceEqual(EncodingBytes("name")));
    }

    [Fact]
    public void Export_WritesTtfFile()
    {
        AafFont font = CreateMinimalFont();
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ttf");

        try
        {
            new TrueTypeFontExporter().Export(font, path, new TrueTypeExportOptions { FamilyName = "TempFont" });

            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static AafFont CreateMinimalFont()
    {
        byte[] header = new byte[AafFormat.HeaderSize];
        header[0] = (byte)'A';
        header[1] = (byte)'A';
        header[2] = (byte)'F';
        header[3] = (byte)'F';
        header[4] = 0x00;
        header[5] = 0x10;

        List<AafGlyph> glyphs = new(AafFormat.GlyphCount);
        for (int i = 0; i < AafFormat.GlyphCount; i++)
        {
            if (i == 65)
            {
                glyphs.Add(new AafGlyph(i, 2, 2, 0, AafFormat.BitmapBaseOffset, [1, 0, 1, 1]));
            }
            else if (i == 32)
            {
                glyphs.Add(new AafGlyph(i, 4, 0, 0, AafFormat.BitmapBaseOffset, []));
            }
            else
            {
                glyphs.Add(new AafGlyph(i, 1, 0, 0, AafFormat.BitmapBaseOffset, []));
            }
        }

        return new AafFont(header, 16, glyphs, "FONTTEST.AAF");
    }

    private static byte[] EncodingBytes(string value) => System.Text.Encoding.ASCII.GetBytes(value);

    private static IEnumerable<byte[]> SplitIntoWords(byte[] bytes)
    {
        for (int i = 0; i <= bytes.Length - 4; i += 4)
        {
            yield return bytes.Skip(i).Take(4).ToArray();
        }
    }
}
