# Static FRM workflow

This sprint adds the first FRM workflow for static/single-frame Fallout 1/2 UI images.

## Scope

Supported now:

- static / single-frame `.FRM` files;
- export to indexed 8-bit BMP using an `.ACT` palette;
- import an edited indexed 8-bit BMP back into a new `.FRM`;
- preservation of frame size, frame offsets, version, FPS, action frame, and direction shifts.

Not supported yet:

- animated FRMs;
- multi-direction creature/object FRMs;
- resizing the FRM frame on import;
- color remapping during import.

## Commands

Inspect a FRM:

```bash
dotnet run --project src/Fallout.Tools.CLI -- frm-info path/to/original.frm
```

Export a static FRM to BMP 8-bit:

```bash
dotnet run --project src/Fallout.Tools.CLI -- frm-export path/to/original.frm exports/original.bmp --act palettes/Default.act
```

Import an edited BMP 8-bit back into a new FRM:

```bash
dotnet run --project src/Fallout.Tools.CLI -- frm-import path/to/original.frm exports/original-edited.bmp exports/original-new.frm
```

## Important notes

The edited BMP must keep the exact same width and height as the original FRM frame.

The import command copies indexed pixel values from the BMP directly into the FRM. It does not convert RGB colors back into palette indices. This is why the editor should export BMP 8-bit using the same ACT palette used by the FRM workflow.

Do not commit original game `.FRM`, `.BMP`, `.PAL`, `.ACT`, `.AAF`, or generated exports to the public repository.
