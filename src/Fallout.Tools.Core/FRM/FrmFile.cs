using System;
using System.Collections.Generic;
using System.Linq;

namespace Fallout.Tools.Core.FRM;

public sealed class FrmFile
{
    public const int DirectionCount = 6;

    public uint Version { get; set; }
    public ushort FramesPerSecond { get; set; }
    public ushort ActionFrame { get; set; }
    public ushort FramesPerDirection { get; set; }
    public short[] ShiftX { get; } = new short[DirectionCount];
    public short[] ShiftY { get; } = new short[DirectionCount];
    public uint[] DirectionOffsets { get; } = new uint[DirectionCount];
    public uint DataSize { get; set; }
    public List<FrmFrame> Frames { get; } = new();

    public FrmFrame FirstFrame
    {
        get
        {
            if (Frames.Count == 0)
            {
                throw new FrmException("FRM file does not contain any frame data.");
            }

            return Frames[0];
        }
    }

    public bool IsStaticSingleFrame => FramesPerDirection == 1 && Frames.Count == 1;

    public FrmFile CreateStaticCopyWithFirstFramePixels(byte[] pixels)
    {
        if (!IsStaticSingleFrame)
        {
            throw new FrmException("Only static/single-frame FRM files are supported by frm-import in this version.");
        }

        FrmFrame sourceFrame = FirstFrame;
        if (pixels.Length != sourceFrame.PixelCount)
        {
            throw new FrmException($"Replacement pixel data has {pixels.Length} bytes, but the FRM frame requires {sourceFrame.PixelCount} bytes.");
        }

        var copy = new FrmFile
        {
            Version = Version,
            FramesPerSecond = FramesPerSecond,
            ActionFrame = ActionFrame,
            FramesPerDirection = 1
        };

        Array.Copy(ShiftX, copy.ShiftX, DirectionCount);
        Array.Copy(ShiftY, copy.ShiftY, DirectionCount);

        for (int i = 0; i < copy.DirectionOffsets.Length; i++)
        {
            copy.DirectionOffsets[i] = 0;
        }

        copy.Frames.Add(new FrmFrame
        {
            Direction = 0,
            Width = sourceFrame.Width,
            Height = sourceFrame.Height,
            OffsetX = sourceFrame.OffsetX,
            OffsetY = sourceFrame.OffsetY,
            Pixels = pixels.ToArray()
        });

        copy.DataSize = checked((uint)(FrmFormat.FrameHeaderSize + pixels.Length));
        return copy;
    }
}

public sealed class FrmFrame
{
    public int Direction { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public short OffsetX { get; set; }
    public short OffsetY { get; set; }
    public byte[] Pixels { get; set; } = Array.Empty<byte>();

    public int PixelCount => Width * Height;
}
