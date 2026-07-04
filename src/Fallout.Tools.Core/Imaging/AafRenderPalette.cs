using Fallout.Tools.Core.AAF;
using SixLabors.ImageSharp.PixelFormats;

namespace Fallout.Tools.Core.Imaging;

public enum AafPaletteKind
{
    Auto,
    Gray,
    Orange,
    Green
}

public sealed class AafRenderPalette
{
    private readonly AafPaletteKind _kind;

    private AafRenderPalette(AafPaletteKind kind)
    {
        _kind = kind;
    }

    public static AafRenderPalette Create(AafPaletteKind kind, string? sourcePath = null)
    {
        if (kind == AafPaletteKind.Auto)
        {
            kind = DetectFromPath(sourcePath);
        }

        return new AafRenderPalette(kind);
    }

    public Rgba32 GetColor(byte value)
    {
        if (value == 0)
        {
            return new Rgba32(0, 0, 0, 0);
        }

        byte normalized = value;
        if (normalized > 9) normalized = 9;

        return _kind switch
        {
            AafPaletteKind.Green => GetGreen(normalized),
            AafPaletteKind.Orange => GetBrightnessColor(184, 156, 40, normalized),
            _ => GetGray(normalized)
        };
    }

    private static AafPaletteKind DetectFromPath(string? sourcePath)
    {
        string fileName = Path.GetFileName(sourcePath ?? string.Empty).ToUpperInvariant();

        if (fileName.Contains("FONT1", StringComparison.OrdinalIgnoreCase))
        {
            return AafPaletteKind.Green;
        }

        if (fileName.Contains("FONT2", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("FONT3", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("FONT4", StringComparison.OrdinalIgnoreCase))
        {
            return AafPaletteKind.Orange;
        }

        return AafPaletteKind.Gray;
    }

    private static Rgba32 GetGray(byte value)
    {
        int v = 40 + value * 22;
        if (v > 255) v = 255;
        return new Rgba32((byte)v, (byte)v, (byte)v, 255);
    }

    private static Rgba32 GetGreen(byte value)
    {
        if (value <= 3) return new Rgba32(0x00, 0x6c, 0x00, 255);
        if (value <= 6) return new Rgba32(0x38, 0xd4, 0x08, 255);
        return new Rgba32(0x3c, 0xf8, 0x00, 255);
    }

    private static Rgba32 GetBrightnessColor(byte baseR, byte baseG, byte baseB, byte value)
    {
        int v = Math.Clamp(value, (byte)1, (byte)9);
        double factor = 0.45 + ((v - 1) / 8.0) * 0.85;

        return new Rgba32(
            ClampToByte((int)Math.Round(baseR * factor)),
            ClampToByte((int)Math.Round(baseG * factor)),
            ClampToByte((int)Math.Round(baseB * factor)),
            255);
    }

    private static byte ClampToByte(int value)
    {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return (byte)value;
    }
}
