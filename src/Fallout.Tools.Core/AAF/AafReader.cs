using System.Text;
using Fallout.Tools.Core.Common;

namespace Fallout.Tools.Core.AAF;

public sealed class AafReader
{
    public AafFont Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));

        using FileStream stream = File.OpenRead(path);
        return Read(stream, path);
    }

    public AafFont Read(Stream stream, string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new AafException("The supplied stream is not readable.");
        }

        if (stream.Length < AafFormat.BitmapBaseOffset)
        {
            throw new AafException($"File too small to be a valid AAF file. Expected at least {AafFormat.BitmapBaseOffset} bytes.");
        }

        using BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        byte[] header = reader.ReadBytes(AafFormat.HeaderSize);
        if (header.Length != AafFormat.HeaderSize)
        {
            throw new AafException("Could not read AAF header.");
        }

        ValidateSignature(header);

        ushort maxHeight = (ushort)((header[4] << 8) | header[5]);

        List<GlyphEntry> entries = new(AafFormat.GlyphCount);
        for (int i = 0; i < AafFormat.GlyphCount; i++)
        {
            ushort width = reader.ReadUInt16BigEndian();
            ushort height = reader.ReadUInt16BigEndian();
            uint dataOffset = reader.ReadUInt32BigEndian();
            entries.Add(new GlyphEntry(width, height, dataOffset));
        }

        List<AafGlyph> glyphs = new(AafFormat.GlyphCount);
        for (int index = 0; index < entries.Count; index++)
        {
            GlyphEntry entry = entries[index];
            ulong pixelCount = (ulong)entry.Width * entry.Height;
            ulong realOffset = (ulong)AafFormat.BitmapBaseOffset + entry.DataOffset;

            if (pixelCount > int.MaxValue)
            {
                throw new AafException($"Glyph[{index}] pixel data is too large: {pixelCount} bytes.");
            }

            if (pixelCount > 0)
            {
                ulong endOffset = realOffset + pixelCount;
                if (realOffset >= (ulong)stream.Length || endOffset > (ulong)stream.Length)
                {
                    throw new AafException(
                        $"Glyph[{index}] has invalid bitmap range. " +
                        $"Offset={realOffset}, Size={pixelCount}, FileLength={stream.Length}.");
                }
            }

            byte[] pixels = Array.Empty<byte>();
            if (pixelCount > 0)
            {
                stream.Position = checked((long)realOffset);
                pixels = reader.ReadBytes(checked((int)pixelCount));
                if (pixels.Length != (int)pixelCount)
                {
                    throw new AafException($"Could not read all pixels for Glyph[{index}].");
                }
            }

            glyphs.Add(new AafGlyph(
                index,
                entry.Width,
                entry.Height,
                entry.DataOffset,
                realOffset,
                pixels));
        }

        return new AafFont(header, maxHeight, glyphs, sourcePath);
    }

    private static void ValidateSignature(byte[] header)
    {
        if (header.Length < 4 || header[0] != 'A' || header[1] != 'A' || header[2] != 'F' || header[3] != 'F')
        {
            throw new AafException("Invalid AAF signature. Expected 'AAFF'.");
        }
    }

    private readonly record struct GlyphEntry(ushort Width, ushort Height, uint DataOffset);
}
