using System.Buffers.Binary;
using System.Text;
using Fallout.Tools.Core.AAF;

namespace Fallout.Tools.Core.Fonts;

public sealed class TrueTypeFontExporter
{
    private const ushort GlyphCount = AafFormat.GlyphCount;
    private const ushort UnitsPerPixelDefault = 64;

    public void Export(AafFont font, string outputPath, TrueTypeExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        options ??= TrueTypeExportOptions.Default;
        byte[] bytes = Build(font, options);

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        File.WriteAllBytes(outputPath, bytes);
    }

    public byte[] Build(AafFont font, TrueTypeExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        options ??= TrueTypeExportOptions.Default;

        ushort unitsPerPixel = options.UnitsPerPixel <= 0 ? UnitsPerPixelDefault : options.UnitsPerPixel;
        ushort unitsPerEm = checked((ushort)Math.Clamp((int)font.MaxHeight * unitsPerPixel, 16, 16384));
        string familyName = string.IsNullOrWhiteSpace(options.FamilyName)
            ? MakeFamilyName(font.SourcePath)
            : options.FamilyName.Trim();

        GlyphBuildResult glyphs = BuildGlyphs(font, unitsPerPixel);

        Dictionary<string, byte[]> tables = new(StringComparer.Ordinal)
        {
            ["cmap"] = BuildCmapTable(),
            ["glyf"] = glyphs.GlyfTable,
            ["head"] = BuildHeadTable(font, unitsPerPixel, unitsPerEm, glyphs.Bounds),
            ["hhea"] = BuildHheaTable(font, unitsPerPixel),
            ["hmtx"] = BuildHmtxTable(font, unitsPerPixel),
            ["loca"] = glyphs.LocaTable,
            ["maxp"] = BuildMaxpTable(glyphs),
            ["name"] = BuildNameTable(familyName),
            ["OS/2"] = BuildOs2Table(font, unitsPerPixel),
            ["post"] = BuildPostTable(unitsPerPixel)
        };

        return BuildSfnt(tables);
    }

    private static string MakeFamilyName(string? sourcePath)
    {
        string name = Path.GetFileNameWithoutExtension(sourcePath ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name)) return "Fallout AAF Font";

        StringBuilder builder = new();
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c is ' ' or '-' or '_') builder.Append(c);
        }

        return builder.Length == 0 ? "Fallout AAF Font" : builder.ToString();
    }

    private static GlyphBuildResult BuildGlyphs(AafFont font, ushort unitsPerPixel)
    {
        List<byte[]> glyphData = new(AafFormat.GlyphCount);
        List<uint> offsets = new(AafFormat.GlyphCount + 1);

        uint currentOffset = 0;
        ushort maxContours = 0;
        ushort maxPoints = 0;
        Bounds overall = Bounds.Empty;

        foreach (AafGlyph glyph in font.Glyphs)
        {
            offsets.Add(currentOffset);
            SimpleGlyph simpleGlyph = BuildSimpleGlyph(font, glyph, unitsPerPixel);
            byte[] data = simpleGlyph.Data;
            glyphData.Add(data);

            if (simpleGlyph.Contours > maxContours) maxContours = simpleGlyph.Contours;
            if (simpleGlyph.Points > maxPoints) maxPoints = simpleGlyph.Points;
            if (!simpleGlyph.Bounds.IsEmpty) overall = Bounds.Union(overall, simpleGlyph.Bounds);

            currentOffset = checked(currentOffset + (uint)data.Length);
            uint padding = PaddingFor4(currentOffset);
            currentOffset += padding;
        }

        offsets.Add(currentOffset);

        using MemoryStream glyf = new();
        foreach (byte[] data in glyphData)
        {
            glyf.Write(data, 0, data.Length);
            Pad4(glyf);
        }

        using MemoryStream loca = new();
        foreach (uint offset in offsets)
        {
            WriteUInt32BE(loca, offset);
        }

        return new GlyphBuildResult(
            glyf.ToArray(),
            loca.ToArray(),
            maxContours,
            maxPoints,
            overall);
    }

    private static SimpleGlyph BuildSimpleGlyph(AafFont font, AafGlyph glyph, ushort unitsPerPixel)
    {
        if (!glyph.HasBitmap)
        {
            return SimpleGlyph.Empty;
        }

        List<Rect> rects = BuildHorizontalRunRects(glyph, unitsPerPixel);
        if (rects.Count == 0)
        {
            return SimpleGlyph.Empty;
        }

        if (rects.Count > short.MaxValue)
        {
            throw new InvalidOperationException($"Glyph {glyph.Index} has too many contours for a simple TrueType glyph.");
        }

        int pointCount = checked(rects.Count * 4);
        if (pointCount > ushort.MaxValue)
        {
            throw new InvalidOperationException($"Glyph {glyph.Index} has too many points for a simple TrueType glyph.");
        }

        Bounds bounds = Bounds.FromRects(rects);

        using MemoryStream stream = new();
        WriteInt16BE(stream, checked((short)rects.Count));
        WriteInt16BE(stream, bounds.XMin);
        WriteInt16BE(stream, bounds.YMin);
        WriteInt16BE(stream, bounds.XMax);
        WriteInt16BE(stream, bounds.YMax);

        for (int i = 0; i < rects.Count; i++)
        {
            WriteUInt16BE(stream, checked((ushort)(i * 4 + 3)));
        }

        WriteUInt16BE(stream, 0); // instructionLength

        for (int i = 0; i < pointCount; i++)
        {
            stream.WriteByte(0x01); // on-curve, signed 16-bit x/y deltas follow
        }

        short previousX = 0;
        foreach (Rect rect in rects)
        {
            // Clockwise rectangle contour.
            short[] xs = [rect.XMin, rect.XMin, rect.XMax, rect.XMax];
            foreach (short x in xs)
            {
                WriteInt16BE(stream, checked((short)(x - previousX)));
                previousX = x;
            }
        }

        short previousY = 0;
        foreach (Rect rect in rects)
        {
            // Clockwise rectangle contour.
            short[] ys = [rect.YMin, rect.YMax, rect.YMax, rect.YMin];
            foreach (short y in ys)
            {
                WriteInt16BE(stream, checked((short)(y - previousY)));
                previousY = y;
            }
        }

        return new SimpleGlyph(stream.ToArray(), checked((ushort)rects.Count), checked((ushort)pointCount), bounds);
    }

    private static List<Rect> BuildHorizontalRunRects(AafGlyph glyph, ushort unitsPerPixel)
    {
        List<Rect> rects = [];

        for (int y = 0; y < glyph.Height; y++)
        {
            int x = 0;
            while (x < glyph.Width)
            {
                while (x < glyph.Width && glyph.Pixels[y * glyph.Width + x] == 0) x++;
                if (x >= glyph.Width) break;

                int startX = x;
                while (x < glyph.Width && glyph.Pixels[y * glyph.Width + x] != 0) x++;
                int endXExclusive = x;

                short xMin = checked((short)(startX * unitsPerPixel));
                short xMax = checked((short)(endXExclusive * unitsPerPixel));
                short yMin = checked((short)((glyph.Height - y - 1) * unitsPerPixel));
                short yMax = checked((short)((glyph.Height - y) * unitsPerPixel));

                rects.Add(new Rect(xMin, yMin, xMax, yMax));
            }
        }

        return rects;
    }

    private static byte[] BuildSfnt(Dictionary<string, byte[]> tables)
    {
        string[] orderedTags = tables.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        ushort numTables = checked((ushort)orderedTags.Length);
        ushort maxPowerOfTwo = HighestPowerOfTwoLessThanOrEqual(numTables);
        ushort searchRange = checked((ushort)(maxPowerOfTwo * 16));
        ushort entrySelector = Log2(maxPowerOfTwo);
        ushort rangeShift = checked((ushort)(numTables * 16 - searchRange));

        uint offset = checked((uint)(12 + numTables * 16));
        List<TableRecord> records = [];
        foreach (string tag in orderedTags)
        {
            byte[] data = tables[tag];
            records.Add(new TableRecord(tag, Checksum(data), offset, checked((uint)data.Length)));
            offset += checked((uint)data.Length);
            offset += PaddingFor4(offset);
        }

        using MemoryStream stream = new();
        WriteUInt32BE(stream, 0x00010000); // sfnt version
        WriteUInt16BE(stream, numTables);
        WriteUInt16BE(stream, searchRange);
        WriteUInt16BE(stream, entrySelector);
        WriteUInt16BE(stream, rangeShift);

        foreach (TableRecord record in records)
        {
            WriteTag(stream, record.Tag);
            WriteUInt32BE(stream, record.Checksum);
            WriteUInt32BE(stream, record.Offset);
            WriteUInt32BE(stream, record.Length);
        }

        foreach (TableRecord record in records)
        {
            byte[] data = tables[record.Tag];
            stream.Write(data, 0, data.Length);
            Pad4(stream);
        }

        byte[] fontBytes = stream.ToArray();
        int headOffset = checked((int)records.Single(r => r.Tag == "head").Offset);
        uint adjustment = unchecked(0xB1B0AFBAu - Checksum(fontBytes));
        BinaryPrimitives.WriteUInt32BigEndian(fontBytes.AsSpan(headOffset + 8, 4), adjustment);

        return fontBytes;
    }

    private static byte[] BuildHeadTable(AafFont font, ushort unitsPerPixel, ushort unitsPerEm, Bounds bounds)
    {
        using MemoryStream stream = new();
        WriteUInt32BE(stream, 0x00010000); // version
        WriteUInt32BE(stream, 0x00010000); // fontRevision
        WriteUInt32BE(stream, 0); // checkSumAdjustment, patched later
        WriteUInt32BE(stream, 0x5F0F3CF5); // magicNumber
        WriteUInt16BE(stream, 0x000B); // flags
        WriteUInt16BE(stream, unitsPerEm);
        WriteInt64BE(stream, 0); // created
        WriteInt64BE(stream, 0); // modified
        WriteInt16BE(stream, bounds.IsEmpty ? (short)0 : bounds.XMin);
        WriteInt16BE(stream, bounds.IsEmpty ? (short)0 : bounds.YMin);
        WriteInt16BE(stream, bounds.IsEmpty ? checked((short)(font.MaxGlyphWidth * unitsPerPixel)) : bounds.XMax);
        WriteInt16BE(stream, bounds.IsEmpty ? checked((short)(font.MaxHeight * unitsPerPixel)) : bounds.YMax);
        WriteUInt16BE(stream, 0); // macStyle
        WriteUInt16BE(stream, 8); // lowestRecPPEM
        WriteInt16BE(stream, 2); // fontDirectionHint
        WriteInt16BE(stream, 1); // indexToLocFormat: long offsets
        WriteInt16BE(stream, 0); // glyphDataFormat
        return stream.ToArray();
    }

    private static byte[] BuildHheaTable(AafFont font, ushort unitsPerPixel)
    {
        short ascent = checked((short)(font.MaxHeight * unitsPerPixel));
        ushort advanceWidthMax = checked((ushort)(Math.Max(1, font.MaxGlyphWidth) * unitsPerPixel));

        using MemoryStream stream = new();
        WriteUInt32BE(stream, 0x00010000); // version
        WriteInt16BE(stream, ascent);
        WriteInt16BE(stream, 0); // descent
        WriteInt16BE(stream, 0); // lineGap
        WriteUInt16BE(stream, advanceWidthMax);
        WriteInt16BE(stream, 0); // minLeftSideBearing
        WriteInt16BE(stream, 0); // minRightSideBearing
        WriteInt16BE(stream, checked((short)advanceWidthMax)); // xMaxExtent
        WriteInt16BE(stream, 1); // caretSlopeRise
        WriteInt16BE(stream, 0); // caretSlopeRun
        WriteInt16BE(stream, 0); // caretOffset
        WriteInt16BE(stream, 0);
        WriteInt16BE(stream, 0);
        WriteInt16BE(stream, 0);
        WriteInt16BE(stream, 0);
        WriteInt16BE(stream, 0); // metricDataFormat
        WriteUInt16BE(stream, GlyphCount); // numberOfHMetrics
        return stream.ToArray();
    }

    private static byte[] BuildHmtxTable(AafFont font, ushort unitsPerPixel)
    {
        using MemoryStream stream = new();
        foreach (AafGlyph glyph in font.Glyphs)
        {
            ushort advance = checked((ushort)(Math.Max(1, (int)glyph.Width) * unitsPerPixel));
            WriteUInt16BE(stream, advance);
            WriteInt16BE(stream, 0); // lsb
        }

        return stream.ToArray();
    }

    private static byte[] BuildMaxpTable(GlyphBuildResult glyphs)
    {
        using MemoryStream stream = new();
        WriteUInt32BE(stream, 0x00010000); // version
        WriteUInt16BE(stream, GlyphCount);
        WriteUInt16BE(stream, glyphs.MaxPoints);
        WriteUInt16BE(stream, glyphs.MaxContours);
        WriteUInt16BE(stream, 0); // maxCompositePoints
        WriteUInt16BE(stream, 0); // maxCompositeContours
        WriteUInt16BE(stream, 2); // maxZones
        WriteUInt16BE(stream, 0); // maxTwilightPoints
        WriteUInt16BE(stream, 0); // maxStorage
        WriteUInt16BE(stream, 0); // maxFunctionDefs
        WriteUInt16BE(stream, 0); // maxInstructionDefs
        WriteUInt16BE(stream, Math.Max((ushort)1, glyphs.MaxPoints)); // maxStackElements
        WriteUInt16BE(stream, 0); // maxSizeOfInstructions
        WriteUInt16BE(stream, 0); // maxComponentElements
        WriteUInt16BE(stream, 0); // maxComponentDepth
        return stream.ToArray();
    }

    private static byte[] BuildCmapTable()
    {
        using MemoryStream subtable = new();
        ushort segCount = 2;
        ushort segCountX2 = checked((ushort)(segCount * 2));
        ushort searchRange = 4;
        ushort entrySelector = 1;
        ushort rangeShift = 0;

        WriteUInt16BE(subtable, 4); // format
        WriteUInt16BE(subtable, 32); // length
        WriteUInt16BE(subtable, 0); // language
        WriteUInt16BE(subtable, segCountX2);
        WriteUInt16BE(subtable, searchRange);
        WriteUInt16BE(subtable, entrySelector);
        WriteUInt16BE(subtable, rangeShift);

        WriteUInt16BE(subtable, 255); // endCode[0]
        WriteUInt16BE(subtable, 0xFFFF); // endCode[1]
        WriteUInt16BE(subtable, 0); // reservedPad
        WriteUInt16BE(subtable, 1); // startCode[0]
        WriteUInt16BE(subtable, 0xFFFF); // startCode[1]
        WriteUInt16BE(subtable, 0); // idDelta[0] => codepoint maps to same glyph index
        WriteUInt16BE(subtable, 1); // idDelta[1], sentinel maps FFFF to 0
        WriteUInt16BE(subtable, 0); // idRangeOffset[0]
        WriteUInt16BE(subtable, 0); // idRangeOffset[1]

        byte[] subtableBytes = subtable.ToArray();

        using MemoryStream cmap = new();
        WriteUInt16BE(cmap, 0); // version
        WriteUInt16BE(cmap, 1); // numberSubtables
        WriteUInt16BE(cmap, 3); // platformID Windows
        WriteUInt16BE(cmap, 1); // encodingID Unicode BMP
        WriteUInt32BE(cmap, 12); // offset
        cmap.Write(subtableBytes, 0, subtableBytes.Length);
        return cmap.ToArray();
    }

    private static byte[] BuildNameTable(string familyName)
    {
        string subfamily = "Regular";
        string unique = familyName + " Regular";
        string fullName = familyName + " Regular";
        string version = "Version 0.2";
        string postScriptName = MakePostScriptName(familyName);

        (ushort NameId, string Value)[] names =
        [
            (0, "Generated by Fallout 1/2 Tools"),
            (1, familyName),
            (2, subfamily),
            (3, unique),
            (4, fullName),
            (5, version),
            (6, postScriptName)
        ];

        using MemoryStream strings = new();
        List<NameRecord> records = [];
        foreach ((ushort nameId, string value) in names)
        {
            byte[] bytes = Encoding.BigEndianUnicode.GetBytes(value);
            records.Add(new NameRecord(nameId, checked((ushort)bytes.Length), checked((ushort)strings.Length)));
            strings.Write(bytes, 0, bytes.Length);
        }

        ushort stringOffset = checked((ushort)(6 + names.Length * 12));
        using MemoryStream table = new();
        WriteUInt16BE(table, 0); // format
        WriteUInt16BE(table, checked((ushort)names.Length));
        WriteUInt16BE(table, stringOffset);

        foreach (NameRecord record in records)
        {
            WriteUInt16BE(table, 3); // platformID Windows
            WriteUInt16BE(table, 1); // encodingID Unicode BMP
            WriteUInt16BE(table, 0x0409); // languageID en-US
            WriteUInt16BE(table, record.NameId);
            WriteUInt16BE(table, record.Length);
            WriteUInt16BE(table, record.Offset);
        }

        byte[] stringBytes = strings.ToArray();
        table.Write(stringBytes, 0, stringBytes.Length);
        return table.ToArray();
    }

    private static string MakePostScriptName(string familyName)
    {
        StringBuilder builder = new();
        foreach (char c in familyName)
        {
            if (char.IsLetterOrDigit(c)) builder.Append(c);
            else if (c is ' ' or '-' or '_') builder.Append('-');
        }

        if (builder.Length == 0) builder.Append("FalloutAAFFont");
        builder.Append("-Regular");
        return builder.ToString();
    }

    private static byte[] BuildOs2Table(AafFont font, ushort unitsPerPixel)
    {
        short ascent = checked((short)(font.MaxHeight * unitsPerPixel));
        ushort winAscent = checked((ushort)(font.MaxHeight * unitsPerPixel));
        short averageWidth = checked((short)Math.Round(font.Glyphs.Average(g => Math.Max(1, (int)g.Width) * unitsPerPixel)));
        ushort firstChar = 1;
        ushort lastChar = 255;

        using MemoryStream stream = new();
        WriteUInt16BE(stream, 4); // version
        WriteInt16BE(stream, averageWidth);
        WriteUInt16BE(stream, 400); // usWeightClass
        WriteUInt16BE(stream, 5); // usWidthClass
        WriteUInt16BE(stream, 0); // fsType installable embedding
        WriteInt16BE(stream, checked((short)(8 * unitsPerPixel))); // ySubscriptXSize
        WriteInt16BE(stream, checked((short)(8 * unitsPerPixel))); // ySubscriptYSize
        WriteInt16BE(stream, 0);
        WriteInt16BE(stream, 0);
        WriteInt16BE(stream, checked((short)(8 * unitsPerPixel))); // ySuperscriptXSize
        WriteInt16BE(stream, checked((short)(8 * unitsPerPixel))); // ySuperscriptYSize
        WriteInt16BE(stream, 0);
        WriteInt16BE(stream, checked((short)(font.MaxHeight * unitsPerPixel / 2)));
        WriteInt16BE(stream, checked((short)Math.Max(1, unitsPerPixel / 8))); // yStrikeoutSize
        WriteInt16BE(stream, checked((short)(font.MaxHeight * unitsPerPixel / 2))); // yStrikeoutPosition
        WriteInt16BE(stream, 0); // sFamilyClass
        stream.Write(new byte[10], 0, 10); // panose
        WriteUInt32BE(stream, 0x00000003); // Basic Latin + Latin-1 Supplement
        WriteUInt32BE(stream, 0);
        WriteUInt32BE(stream, 0);
        WriteUInt32BE(stream, 0);
        stream.Write(Encoding.ASCII.GetBytes("F12T"), 0, 4); // achVendID
        WriteUInt16BE(stream, 0x0040); // fsSelection regular
        WriteUInt16BE(stream, firstChar);
        WriteUInt16BE(stream, lastChar);
        WriteInt16BE(stream, ascent); // sTypoAscender
        WriteInt16BE(stream, 0); // sTypoDescender
        WriteInt16BE(stream, 0); // sTypoLineGap
        WriteUInt16BE(stream, winAscent);
        WriteUInt16BE(stream, 0); // usWinDescent
        WriteUInt32BE(stream, 0x00000001); // code page 1252
        WriteUInt32BE(stream, 0);
        WriteInt16BE(stream, checked((short)(font.MaxHeight * unitsPerPixel / 2))); // sxHeight
        WriteInt16BE(stream, ascent); // sCapHeight
        WriteUInt16BE(stream, 0); // usDefaultChar
        WriteUInt16BE(stream, 32); // usBreakChar
        WriteUInt16BE(stream, 1); // usMaxContext
        return stream.ToArray();
    }

    private static byte[] BuildPostTable(ushort unitsPerPixel)
    {
        using MemoryStream stream = new();
        WriteUInt32BE(stream, 0x00030000); // format 3.0, no glyph names
        WriteUInt32BE(stream, 0); // italicAngle
        WriteInt16BE(stream, checked((short)-Math.Max(1, unitsPerPixel / 8))); // underlinePosition
        WriteInt16BE(stream, checked((short)Math.Max(1, unitsPerPixel / 8))); // underlineThickness
        WriteUInt32BE(stream, 0); // isFixedPitch
        WriteUInt32BE(stream, 0); // minMemType42
        WriteUInt32BE(stream, 0); // maxMemType42
        WriteUInt32BE(stream, 0); // minMemType1
        WriteUInt32BE(stream, 0); // maxMemType1
        return stream.ToArray();
    }

    private static ushort HighestPowerOfTwoLessThanOrEqual(ushort value)
    {
        ushort result = 1;
        while (result * 2 <= value) result *= 2;
        return result;
    }

    private static ushort Log2(ushort value)
    {
        ushort result = 0;
        while (value > 1)
        {
            value /= 2;
            result++;
        }

        return result;
    }

    private static uint PaddingFor4(uint length) => (4 - (length % 4)) % 4;

    private static void Pad4(Stream stream)
    {
        while (stream.Length % 4 != 0) stream.WriteByte(0);
    }

    private static uint Checksum(byte[] data)
    {
        uint sum = 0;
        int paddedLength = (data.Length + 3) & ~3;
        Span<byte> buffer = stackalloc byte[4];

        for (int i = 0; i < paddedLength; i += 4)
        {
            buffer.Clear();
            int remaining = Math.Min(4, data.Length - i);
            if (remaining > 0)
            {
                data.AsSpan(i, remaining).CopyTo(buffer);
            }

            sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(buffer));
        }

        return sum;
    }

    private static void WriteTag(Stream stream, string tag)
    {
        if (tag.Length != 4) throw new ArgumentException("TrueType table tags must contain exactly 4 characters.", nameof(tag));
        byte[] bytes = Encoding.ASCII.GetBytes(tag);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteUInt16BE(Stream stream, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteInt16BE(Stream stream, short value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteUInt32BE(Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteInt64BE(Stream stream, long value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private readonly record struct TableRecord(string Tag, uint Checksum, uint Offset, uint Length);
    private readonly record struct NameRecord(ushort NameId, ushort Length, ushort Offset);

    private readonly record struct GlyphBuildResult(
        byte[] GlyfTable,
        byte[] LocaTable,
        ushort MaxContours,
        ushort MaxPoints,
        Bounds Bounds);

    private readonly record struct SimpleGlyph(byte[] Data, ushort Contours, ushort Points, Bounds Bounds)
    {
        public static SimpleGlyph Empty { get; } = new(Array.Empty<byte>(), 0, 0, Bounds.Empty);
    }

    private readonly record struct Rect(short XMin, short YMin, short XMax, short YMax);

    private readonly record struct Bounds(short XMin, short YMin, short XMax, short YMax, bool IsEmpty)
    {
        public static Bounds Empty { get; } = new(0, 0, 0, 0, true);

        public static Bounds FromRects(IReadOnlyList<Rect> rects)
        {
            if (rects.Count == 0) return Empty;

            short xMin = rects[0].XMin;
            short yMin = rects[0].YMin;
            short xMax = rects[0].XMax;
            short yMax = rects[0].YMax;

            foreach (Rect rect in rects)
            {
                if (rect.XMin < xMin) xMin = rect.XMin;
                if (rect.YMin < yMin) yMin = rect.YMin;
                if (rect.XMax > xMax) xMax = rect.XMax;
                if (rect.YMax > yMax) yMax = rect.YMax;
            }

            return new Bounds(xMin, yMin, xMax, yMax, false);
        }

        public static Bounds Union(Bounds a, Bounds b)
        {
            if (a.IsEmpty) return b;
            if (b.IsEmpty) return a;

            return new Bounds(
                Math.Min(a.XMin, b.XMin),
                Math.Min(a.YMin, b.YMin),
                Math.Max(a.XMax, b.XMax),
                Math.Max(a.YMax, b.YMax),
                false);
        }
    }
}

public sealed class TrueTypeExportOptions
{
    public string? FamilyName { get; init; }

    public ushort UnitsPerPixel { get; init; } = 64;

    public static TrueTypeExportOptions Default { get; } = new();
}
