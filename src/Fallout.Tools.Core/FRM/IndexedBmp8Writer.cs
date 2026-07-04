using System;
using System.IO;
using SixLabors.ImageSharp.PixelFormats;

namespace Fallout.Tools.Core.FRM;

public sealed class IndexedBmp8Writer
{
    public void Write(string path, IndexedImage image, IReadOnlyList<Rgba32> palette)
    {
        if (palette.Count < 256)
        {
            throw new ArgumentException("The BMP palette must contain 256 colors.", nameof(palette));
        }

        string? outputDirectory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        int width = image.Width;
        int height = image.Height;
        int stride = ((width + 3) / 4) * 4;
        int pixelDataSize = stride * height;
        const int fileHeaderSize = 14;
        const int infoHeaderSize = 40;
        const int paletteSize = 256 * 4;
        int pixelDataOffset = fileHeaderSize + infoHeaderSize + paletteSize;
        int fileSize = pixelDataOffset + pixelDataSize;

        byte[] row = new byte[stride];

        using FileStream stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(pixelDataOffset);

        writer.Write(infoHeaderSize);
        writer.Write(width);
        writer.Write(height);
        writer.Write((ushort)1);
        writer.Write((ushort)8);
        writer.Write(0);
        writer.Write(pixelDataSize);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(256);
        writer.Write(0);

        for (int i = 0; i < 256; i++)
        {
            Rgba32 color = palette[i];
            writer.Write(color.B);
            writer.Write(color.G);
            writer.Write(color.R);
            writer.Write((byte)0);
        }

        // BMP with a positive height is stored bottom-up. The IndexedImage buffer
        // is top-down, matching how FRM frame pixels are read.
        for (int y = height - 1; y >= 0; y--)
        {
            Array.Clear(row, 0, row.Length);
            Buffer.BlockCopy(image.Pixels, y * width, row, 0, width);
            writer.Write(row);
        }
    }
}
