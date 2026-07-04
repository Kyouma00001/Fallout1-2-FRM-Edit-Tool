namespace Fallout.Tools.Core.Imaging;

public sealed record UiTextPlacement(
    string Name,
    int X,
    int Y,
    int Width,
    UiTextAlignment Alignment,
    string Text);
