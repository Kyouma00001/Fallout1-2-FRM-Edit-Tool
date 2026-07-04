namespace Fallout.Tools.Core.AAF;

public static class AafFormat
{
    public const string Signature = "AAFF";
    public const int HeaderSize = 0x000C;
    public const int GlyphCount = 256;
    public const int GlyphEntrySize = 8;
    public const int GlyphTableOffset = HeaderSize;
    public const int BitmapBaseOffset = HeaderSize + GlyphCount * GlyphEntrySize; // 0x080C
}
