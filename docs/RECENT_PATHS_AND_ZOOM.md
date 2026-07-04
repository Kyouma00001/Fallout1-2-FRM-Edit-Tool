# Recent Paths and Zoom

This sprint improves editor usability with persistent file dialog locations and visual zoom.

## Recent paths

The editor remembers the last folder used for each picker type:

- image files
- FRM files
- AAF fonts
- ACT palettes
- project files
- layout files
- BMP export
- FRM export
- PNG preview export

Settings are saved per user outside the repository:

```text
%AppData%\Fallout.Tools.UI\editor-settings.json
```

This file must not be committed.

## Zoom

The toolbar now includes a Zoom section:

- Zoom -
- 100%
- Zoom +
- current zoom percentage

Zoom affects only the editor preview. It does not change X/Y coordinates, image dimensions, BMP export, or FRM export.

Supported zoom range:

```text
25% to 800%
```

## Validation

Run:

```bash
dotnet build
dotnet test
dotnet run --project src/Fallout.Tools.UI
```

Manual checks:

1. Open ACT from a folder.
2. Close and reopen the picker; it should start from that ACT folder.
3. Repeat for FRM, AAF, image, project, and exports.
4. Open a FRM/BMP and use Zoom + / Zoom - / 100%.
5. Move text and erase patches while zoomed in.
6. Export BMP/FRM and confirm output dimensions are unchanged.
