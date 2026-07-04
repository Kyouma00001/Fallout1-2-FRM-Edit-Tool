using System;
using System.IO;

namespace Fallout.Tools.Core.FRM;

public sealed class IndexedBmp8Reader
{
    public IndexedImage Read(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        if (reader.ReadByte() != (byte)'B' || reader.ReadByte() != (byte)'M')
        {
            throw new InvalidDataException("File is not a BMP.");
        }

        _ = reader.ReadUInt32(); // file size
        _ = reader.ReadUInt16();
        _ = reader.ReadUInt16();
        uint pixelOffset = reader.ReadUInt32();

        uint dibHeaderSize = reader.ReadUInt32();
        if (dibHeaderSize < 40)
        {
            throw new InvalidDataException("Unsupported BMP DIB header.");
        }

        int width = reader.ReadInt32();
        int signedHeight = reader.ReadInt32();
        ushort planes = reader.ReadUInt16();
        ushort bitsPerPixel = reader.ReadUInt16();
        uint compression = reader.ReadUInt32();
        _ = reader.ReadUInt32(); // image size
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        uint colorsUsed = reader.ReadUInt32();
        _ = reader.ReadUInt32();

        if (planes != 1 || bitsPerPixel != 8)
        {
            throw new InvalidDataException("BMP must be 8-bit indexed.");
        }

        if (compression != 0)
        {
            throw new InvalidDataException("Compressed BMP files are not supported.");
        }

        if (width <= 0 || signedHeight == 0)
        {
            throw new InvalidDataException("Invalid BMP dimensions.");
        }

        bool topDown = signedHeight < 0;
        int height = Math.Abs(signedHeight);
        int paletteEntries = colorsUsed == 0 ? 256 : checked((int)colorsUsed);

        long paletteBytes = paletteEntries * 4L;
        long expectedPixelOffsetMin = 14L + dibHeaderSize + paletteBytes;
        if (pixelOffset < expectedPixelOffsetMin)
        {
            // Some encoders leave colorsUsed as zero even with a full palette. If the
            // offset is sane for a 256 color table, accept it.
            if (pixelOffset < 14L + dibHeaderSize)
            {
                throw new InvalidDataException("Invalid BMP pixel data offset.");
            }
        }

        int stride = ((width + 3) / 4) * 4;
        var pixels = new byte[checked(width * height)];
        var row = new byte[stride];

        stream.Position = pixelOffset;
        for (int storedY = 0; storedY < height; storedY++)
        {
            int read = stream.Read(row, 0, row.Length);
            if (read != row.Length)
            {
                throw new EndOfStreamException("BMP pixel data is truncated.");
            }

            int targetY = topDown ? storedY : height - 1 - storedY;
            Buffer.BlockCopy(row, 0, pixels, targetY * width, width);
        }

        return new IndexedImage(width, height, pixels);
    }
}
