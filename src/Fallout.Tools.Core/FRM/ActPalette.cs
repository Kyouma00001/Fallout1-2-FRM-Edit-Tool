using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp.PixelFormats;

namespace Fallout.Tools.Core.FRM;

public sealed class ActPalette
{
    public const int ColorCount = 256;
    public const int ActSize = ColorCount * 3;

    public ActPalette(IReadOnlyList<Rgba32> colors)
    {
        if (colors.Count < ColorCount)
        {
            throw new ArgumentException("ACT palette must contain 256 colors.", nameof(colors));
        }

        Colors = colors.Take(ColorCount).ToArray();
    }

    public IReadOnlyList<Rgba32> Colors { get; }

    public static ActPalette Load(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        if (data.Length < ActSize)
        {
            throw new InvalidDataException("ACT palette must contain at least 768 bytes: 256 RGB colors.");
        }

        var colors = new Rgba32[ColorCount];
        for (int i = 0; i < colors.Length; i++)
        {
            int offset = i * 3;
            colors[i] = new Rgba32(data[offset], data[offset + 1], data[offset + 2], 255);
        }

        return new ActPalette(colors);
    }
}
