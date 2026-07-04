using Fallout.Tools.Core.AAF;

namespace Fallout.Tools.Tests;

public sealed class AafReaderTests
{
    [Fact]
    public void Read_ValidMinimalAaf_ReturnsExpectedMetadataAndGlyph()
    {
        byte[] data = CreateMinimalAaf();

        using MemoryStream stream = new MemoryStream(data);
        AafFont font = new AafReader().Read(stream, "minimal.aaf");

        Assert.Equal(16, font.MaxHeight);
        Assert.Equal(256, font.Glyphs.Count);
        Assert.Equal(1, font.NonEmptyGlyphCount);

        AafGlyph glyph = font.Glyphs[65];
        Assert.Equal(2, glyph.Width);
        Assert.Equal(2, glyph.Height);
        Assert.Equal<uint>(0, glyph.DataOffset);
        Assert.Equal((ulong)AafFormat.BitmapBaseOffset, glyph.RealOffset);
        Assert.Equal(new byte[] { 0, 1, 2, 0 }, glyph.Pixels);
    }

    [Fact]
    public void Read_InvalidSignature_ThrowsAafException()
    {
        byte[] data = CreateMinimalAaf();
        data[0] = (byte)'B';

        using MemoryStream stream = new MemoryStream(data);

        Assert.Throws<AafException>(() => new AafReader().Read(stream, "bad.aaf"));
    }

    private static byte[] CreateMinimalAaf()
    {
        byte[] data = new byte[AafFormat.BitmapBaseOffset + 4];

        data[0] = (byte)'A';
        data[1] = (byte)'A';
        data[2] = (byte)'F';
        data[3] = (byte)'F';
        data[4] = 0x00;
        data[5] = 0x10;
        data[6] = 0x00;
        data[7] = 0x01;
        data[8] = 0x00;
        data[9] = 0x04;
        data[10] = 0x00;
        data[11] = 0x04;

        int glyph65 = AafFormat.HeaderSize + 65 * AafFormat.GlyphEntrySize;
        WriteUInt16BE(data, glyph65, 2);       // width
        WriteUInt16BE(data, glyph65 + 2, 2);   // height
        WriteUInt32BE(data, glyph65 + 4, 0);   // offset

        data[AafFormat.BitmapBaseOffset + 0] = 0;
        data[AafFormat.BitmapBaseOffset + 1] = 1;
        data[AafFormat.BitmapBaseOffset + 2] = 2;
        data[AafFormat.BitmapBaseOffset + 3] = 0;

        return data;
    }

    private static void WriteUInt16BE(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)((value >> 8) & 0xFF);
        data[offset + 1] = (byte)(value & 0xFF);
    }

    private static void WriteUInt32BE(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)((value >> 24) & 0xFF);
        data[offset + 1] = (byte)((value >> 16) & 0xFF);
        data[offset + 2] = (byte)((value >> 8) & 0xFF);
        data[offset + 3] = (byte)(value & 0xFF);
    }
}
