using System;
using System.IO;
using System.Text;

namespace Fallout.Tools.Core.FRM;

public sealed class FrmWriter
{
    public void Write(string path, FrmFile file)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Output path is required.", nameof(path));
        }

        string? outputDirectory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        using FileStream stream = File.Create(path);
        Write(stream, file);
    }

    public void Write(Stream stream, FrmFile file)
    {
        if (file.Frames.Count != 1 || file.FramesPerDirection != 1)
        {
            throw new FrmException("FrmWriter currently writes static/single-frame FRM files only.");
        }

        FrmFrame frame = file.FirstFrame;
        if (frame.Pixels.Length != frame.PixelCount)
        {
            throw new FrmException($"Frame pixel buffer has {frame.Pixels.Length} bytes, but {frame.Width}x{frame.Height} requires {frame.PixelCount} bytes.");
        }

        file.DataSize = checked((uint)(FrmFormat.FrameHeaderSize + frame.Pixels.Length));

        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        BigEndianBinary.WriteUInt32(writer, file.Version);
        BigEndianBinary.WriteUInt16(writer, file.FramesPerSecond);
        BigEndianBinary.WriteUInt16(writer, file.ActionFrame);
        BigEndianBinary.WriteUInt16(writer, 1);

        for (int i = 0; i < FrmFormat.DirectionCount; i++)
        {
            BigEndianBinary.WriteInt16(writer, file.ShiftX[i]);
        }

        for (int i = 0; i < FrmFormat.DirectionCount; i++)
        {
            BigEndianBinary.WriteInt16(writer, file.ShiftY[i]);
        }

        for (int i = 0; i < FrmFormat.DirectionCount; i++)
        {
            BigEndianBinary.WriteUInt32(writer, 0);
        }

        BigEndianBinary.WriteUInt32(writer, file.DataSize);

        BigEndianBinary.WriteUInt16(writer, frame.Width);
        BigEndianBinary.WriteUInt16(writer, frame.Height);
        BigEndianBinary.WriteUInt32(writer, checked((uint)frame.Pixels.Length));
        BigEndianBinary.WriteInt16(writer, frame.OffsetX);
        BigEndianBinary.WriteInt16(writer, frame.OffsetY);
        writer.Write(frame.Pixels);
    }
}
