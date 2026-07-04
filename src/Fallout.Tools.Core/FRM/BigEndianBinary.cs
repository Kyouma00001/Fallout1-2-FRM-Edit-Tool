using System;
using System.Buffers.Binary;
using System.IO;

namespace Fallout.Tools.Core.FRM;

internal static class BigEndianBinary
{
    public static ushort ReadUInt16(BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[2];
        Fill(reader, buffer);
        return BinaryPrimitives.ReadUInt16BigEndian(buffer);
    }

    public static short ReadInt16(BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[2];
        Fill(reader, buffer);
        return BinaryPrimitives.ReadInt16BigEndian(buffer);
    }

    public static uint ReadUInt32(BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[4];
        Fill(reader, buffer);
        return BinaryPrimitives.ReadUInt32BigEndian(buffer);
    }

    public static void WriteUInt16(BinaryWriter writer, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        writer.Write(buffer);
    }

    public static void WriteInt16(BinaryWriter writer, short value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
        writer.Write(buffer);
    }

    public static void WriteUInt32(BinaryWriter writer, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        writer.Write(buffer);
    }

    private static void Fill(BinaryReader reader, Span<byte> buffer)
    {
        int read = reader.Read(buffer);
        if (read != buffer.Length)
        {
            throw new EndOfStreamException("Unexpected end of FRM file.");
        }
    }
}
