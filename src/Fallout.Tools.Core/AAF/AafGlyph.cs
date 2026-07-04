namespace Fallout.Tools.Core.AAF;

public sealed class AafGlyph
{
    public AafGlyph(int index, ushort width, ushort height, uint dataOffset, ulong realOffset, byte[] pixels)
    {
        Index = index;
        Width = width;
        Height = height;
        DataOffset = dataOffset;
        RealOffset = realOffset;
        Pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
    }

    public int Index { get; }

    public ushort Width { get; }

    public ushort Height { get; }

    public uint DataOffset { get; }

    public ulong RealOffset { get; }

    public byte[] Pixels { get; }

    public bool HasBitmap => Width > 0 && Height > 0 && Pixels.Length > 0;

    public bool HasAdvanceOnly => Width > 0 && Height == 0;

    public int PixelCount => checked(Width * Height);

    public string DisplayName => AafGlyphNames.GetDisplayName(Index);
}
