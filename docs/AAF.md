# AAF Notes

## Rendering note

The AAF files contain indexed pixel data with multiple brightness values. This means the original Fallout UI font is not just a monochrome shape; the glyphs include texture/shading information.

A conventional TTF export is useful for approximate layout in applications like Photoshop, but it cannot preserve the multi-tone pixel texture. For faithful UI translation work, use the `render` command to generate PNG text directly from the AAF glyph pixels.

Example:

```bash
dotnet run --project src/Fallout.Tools.CLI -- render samples/FONT4.AAF "BARTER" exports/BARTER.png --scale 1 --palette orange
```
