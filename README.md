# Fallout 1/2 Tools - TTF Sprint 2.1 Hotfix

This hotfix keeps the AAF -> TTF exporter, but adds a pixel-perfect PNG text renderer.

Why: a classic TrueType font can only store monochrome outlines. It cannot preserve the original Fallout AAF multi-tone pixel texture. For UI translation work, rendering text from the original AAF glyph pixels into PNG is more faithful than using the TTF directly.

## New command

```bash
dotnet run --project src/Fallout.Tools.CLI -- render samples/FONT4.AAF BARTER exports/BARTER.png --scale 1 --palette orange
```

For text with spaces, use quotes:

```bash
dotnet run --project src/Fallout.Tools.CLI -- render samples/FONT4.AAF "SAVE GLYPH" exports/SAVE_GLYPH.png --scale 1 --palette orange
```

Useful options:

```bash
--scale 1
--palette auto|gray|orange|green
--letter-spacing 0
--line-spacing 0
```

Use `--scale 1` for direct FRM/BMP editing. Use higher scale values only for previewing.
