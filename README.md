# Fallout 1/2 Tools

Open-source tools for Fallout 1, Fallout 2, and related classic Fallout mods.

## MVP 1: FalloutFontTool

The first MVP focuses on Fallout `.AAF` fonts:

- Read `.AAF` font files.
- Print font metadata.
- Export every glyph as PNG.
- Export a 16x16 atlas PNG.

## Quick start

```bash
dotnet restore
dotnet build
```

Show font information:

```bash
dotnet run --project src/Fallout.Tools.CLI -- info samples/FONT3.AAF
```

Export glyphs:

```bash
dotnet run --project src/Fallout.Tools.CLI -- export samples/FONT3.AAF exports/FONT3 --scale 4
```

Run tests:

```bash
dotnet test
```

## Asset note

Do not commit original Fallout game assets such as `.AAF`, `.FON`, `.PAL`, `.DAT`, or `.FRM` files. Keep them locally in `samples/` for testing.
