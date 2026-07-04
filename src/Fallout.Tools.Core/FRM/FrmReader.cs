using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fallout.Tools.Core.FRM;

public sealed class FrmReader
{
    public FrmFile Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Input path is required.", nameof(path));
        }

        using FileStream stream = File.OpenRead(path);
        return Read(stream);
    }

    public FrmFile Read(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new FrmException("FRM stream must be seekable.");
        }

        if (stream.Length < FrmFormat.HeaderSize + FrmFormat.FrameHeaderSize)
        {
            throw new FrmException("File is too small to be a valid FRM.");
        }

        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        var file = new FrmFile
        {
            Version = BigEndianBinary.ReadUInt32(reader),
            FramesPerSecond = BigEndianBinary.ReadUInt16(reader),
            ActionFrame = BigEndianBinary.ReadUInt16(reader),
            FramesPerDirection = BigEndianBinary.ReadUInt16(reader)
        };

        if (file.FramesPerDirection == 0)
        {
            throw new FrmException("Invalid FRM: frames per direction is zero.");
        }

        for (int i = 0; i < FrmFormat.DirectionCount; i++)
        {
            file.ShiftX[i] = BigEndianBinary.ReadInt16(reader);
        }

        for (int i = 0; i < FrmFormat.DirectionCount; i++)
        {
            file.ShiftY[i] = BigEndianBinary.ReadInt16(reader);
        }

        for (int i = 0; i < FrmFormat.DirectionCount; i++)
        {
            file.DirectionOffsets[i] = BigEndianBinary.ReadUInt32(reader);
        }

        file.DataSize = BigEndianBinary.ReadUInt32(reader);

        long frameDataStart = stream.Position;
        long frameDataEnd = stream.Length;
        if (file.DataSize > 0 && frameDataStart + file.DataSize <= stream.Length)
        {
            frameDataEnd = frameDataStart + file.DataSize;
        }

        foreach ((int direction, uint offset) in GetDirectionOffsetsToRead(file, frameDataEnd - frameDataStart))
        {
            stream.Position = frameDataStart + offset;
            for (int frameIndex = 0; frameIndex < file.FramesPerDirection; frameIndex++)
            {
                if (stream.Position + FrmFormat.FrameHeaderSize > frameDataEnd)
                {
                    break;
                }

                FrmFrame frame = ReadFrame(reader, direction, frameDataEnd);
                file.Frames.Add(frame);
            }
        }

        if (file.Frames.Count == 0)
        {
            throw new FrmException("No readable FRM frames were found.");
        }

        return file;
    }

    private static FrmFrame ReadFrame(BinaryReader reader, int direction, long frameDataEnd)
    {
        var frame = new FrmFrame
        {
            Direction = direction,
            Width = BigEndianBinary.ReadUInt16(reader),
            Height = BigEndianBinary.ReadUInt16(reader)
        };

        uint frameSize = BigEndianBinary.ReadUInt32(reader);
        frame.OffsetX = BigEndianBinary.ReadInt16(reader);
        frame.OffsetY = BigEndianBinary.ReadInt16(reader);

        int expectedSize = checked(frame.Width * frame.Height);
        if (frameSize != expectedSize)
        {
            throw new FrmException($"Unsupported FRM frame size. Header says {frameSize} bytes, but {frame.Width}x{frame.Height} requires {expectedSize} bytes.");
        }

        if (reader.BaseStream.Position + frameSize > frameDataEnd)
        {
            throw new FrmException("FRM frame pixel data is truncated.");
        }

        frame.Pixels = reader.ReadBytes((int)frameSize);
        if (frame.Pixels.Length != frameSize)
        {
            throw new FrmException("FRM frame pixel data is truncated.");
        }

        return frame;
    }

    private static IEnumerable<(int Direction, uint Offset)> GetDirectionOffsetsToRead(FrmFile file, long frameDataLength)
    {
        var seen = new HashSet<uint>();

        // Direction 0 usually starts at offset 0. In static UI FRMs the remaining
        // direction offsets are often also zero, so read offset 0 only once.
        yield return (0, 0);
        seen.Add(0);

        for (int direction = 1; direction < FrmFormat.DirectionCount; direction++)
        {
            uint offset = file.DirectionOffsets[direction];
            if (offset == 0 || offset >= frameDataLength || !seen.Add(offset))
            {
                continue;
            }

            yield return (direction, offset);
        }
    }
}
