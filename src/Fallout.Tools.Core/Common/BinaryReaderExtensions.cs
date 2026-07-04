namespace Fallout.Tools.Core.Common;

public static class BinaryReaderExtensions
{
    public static ushort ReadUInt16BigEndian(this BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        int high = reader.ReadByte();
        int low = reader.ReadByte();
        return (ushort)((high << 8) | low);
    }

    public static uint ReadUInt32BigEndian(this BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        uint b0 = reader.ReadByte();
        uint b1 = reader.ReadByte();
        uint b2 = reader.ReadByte();
        uint b3 = reader.ReadByte();
        return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
    }
}
