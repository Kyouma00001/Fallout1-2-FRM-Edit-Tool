using Fallout.Tools.Core.AAF;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Fallout.Tools.Core.Imaging;

public sealed class UiTextComposer
{
    private readonly AafTextRenderer _textRenderer;

    public UiTextComposer(AafTextRenderer textRenderer)
    {
        _textRenderer = textRenderer ?? throw new ArgumentNullException(nameof(textRenderer));
    }

    public Image<Rgba32> Compose(
        Image<Rgba32> background,
        AafFont font,
        IEnumerable<UiTextPlacement> placements,
        AafTextRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(background);
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(placements);

        options ??= AafTextRenderOptions.Default;

        Image<Rgba32> output = background.Clone();

        foreach (UiTextPlacement placement in placements)
        {
            using Image<Rgba32> textImage = _textRenderer.RenderText(font, placement.Text, options);

            int x = ResolveX(placement, textImage.Width);
            Overlay(output, textImage, x, placement.Y);
        }

        return output;
    }

    private static int ResolveX(UiTextPlacement placement, int textWidth)
    {
        if (placement.Width <= 0)
        {
            return placement.X;
        }

        return placement.Alignment switch
        {
            UiTextAlignment.Center => placement.X + Math.Max(0, (placement.Width - textWidth) / 2),
            UiTextAlignment.Right => placement.X + Math.Max(0, placement.Width - textWidth),
            _ => placement.X
        };
    }

    private static void Overlay(Image<Rgba32> target, Image<Rgba32> overlay, int startX, int startY)
    {
        for (int y = 0; y < overlay.Height; y++)
        {
            int targetY = startY + y;
            if (targetY < 0 || targetY >= target.Height)
            {
                continue;
            }

            for (int x = 0; x < overlay.Width; x++)
            {
                int targetX = startX + x;
                if (targetX < 0 || targetX >= target.Width)
                {
                    continue;
                }

                Rgba32 source = overlay[x, y];
                if (source.A == 0)
                {
                    continue;
                }

                target[targetX, targetY] = source;
            }
        }
    }
}
