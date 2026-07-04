# Fallout 1/2 Tools

Open-source tools for inspecting, exporting, composing, and editing static Fallout 1/2 UI assets during localization work.

The main workflow is focused on Fallout 1/2 and Fallout Et Tu interface translation: inspect fonts, render `.AAF` text, preview/edit static `.FRM` UI screens, export indexed BMP files with the correct ACT palette, and safely export edited static FRM files.

## Current features

### Command line tools

- Inspect `.AAF` font metadata.
- Export `.AAF` glyphs and atlas PNGs.
- Render text from `.AAF` fonts to PNG.
- Batch-render translated UI strings.
- Compose translated text over a base PNG/BMP image.
- Export experimental `.AAF` to `.TTF`.
- Inspect static `.FRM` files.
- Export static `.FRM` to 8-bit indexed BMP using an ACT palette.
- Import edited indexed BMP pixels back into a static `.FRM` template.

### Visual editor

- Open ACT palettes.
- Open static FRM files as editable previews.
- Open PNG/BMP base images.
- Open AAF fonts for text rendering.
- Add and move text objects visually.
- Resize text width and scale with handles.
- Add erase/clone patches to cover original text.
- Move erase target/source patches with mouse and keyboard.
- Save/load `.fui.json` editor projects.
- Export PNG preview, indexed BMP, and edited static FRM.
- Prevent accidental overwriting of source FRM/base images.
- Remember recent directories for file pickers.
- Zoom with buttons or mouse wheel.
- Pan the canvas by dragging the background.
- Use a Fallout-inspired dark/amber editor theme.

## Requirements

- .NET 8 SDK
- Windows is the primary tested platform for the visual editor.

## Build and test

```bash
dotnet restore
dotnet build
dotnet test
```

Release validation:

```bash
dotnet clean
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet list package --vulnerable
```

## Run the visual editor

```bash
dotnet run --project src/Fallout.Tools.UI
```

Recommended visual workflow:

1. Open ACT.
2. Open FRM.
3. Open AAF.
4. Add erase patch over the original text.
5. Move the source patch to a clean matching texture area.
6. Add translated text.
7. Save a `.fui.json` project.
8. Export BMP 8-bit or edited FRM.
9. Test the edited FRM in-game.

## CLI examples

Inspect an AAF font:

```bash
dotnet run --project src/Fallout.Tools.CLI -- info path/to/FONT4.AAF
```

Export glyphs and atlas:

```bash
dotnet run --project src/Fallout.Tools.CLI -- export path/to/FONT4.AAF exports/FONT4 --scale 4 --palette orange
```

Render text:

```bash
dotnet run --project src/Fallout.Tools.CLI -- render path/to/FONT4.AAF "PRONTO" exports/PRONTO.png --scale 1 --palette orange --uppercase
```

Batch-render text:

```bash
dotnet run --project src/Fallout.Tools.CLI -- render-batch path/to/FONT4.AAF samples/render-batch.example.txt exports/ui-text --scale 1 --palette orange --uppercase
```

Compose text over a base image:

```bash
dotnet run --project src/Fallout.Tools.CLI -- compose-ui path/to/FONT4.AAF path/to/base.png samples/ui-compose.example.txt exports/ui-composed.png --scale 1 --palette orange --uppercase
```

Inspect a static FRM:

```bash
dotnet run --project src/Fallout.Tools.CLI -- frm-info path/to/interface.frm
```

Export a static FRM to indexed BMP:

```bash
dotnet run --project src/Fallout.Tools.CLI -- frm-export path/to/interface.frm exports/interface.bmp --act path/to/Default.act
```

Import edited indexed BMP pixels into a new FRM:

```bash
dotnet run --project src/Fallout.Tools.CLI -- frm-import path/to/original.frm exports/interface-edited.bmp exports/interface-edited.frm
```

## Asset policy

Do not commit original or derived game assets to the repository.

Keep these files outside Git unless they are clearly synthetic test fixtures that are safe to redistribute:

- `.FRM`
- `.AAF`
- `.ACT`
- `.PAL`
- `.BMP`
- `.PNG` exports/previews
- `.TTF` generated fonts
- `.fui.json` projects containing local asset paths

Before opening a PR, run:

```bash
git status --short
git ls-files | grep -Ei '\.(frm|aaf|act|pal|bmp|png|ttf)$'
```

The second command should normally return nothing.

## Documentation

Important docs live in `docs/`, including:

- `FRM_STATIC_WORKFLOW.md`
- `FRM_VISUAL_EDITOR.md`
- `INDEXED_BMP_EXPORT.md`
- `PROJECT_FILES.md`
- `SAFETY_RELEASE.md`
- `FALLOUT_UI_THEME.md`
- `RECENT_PATHS_AND_ZOOM.md`
- `MOUSE_ZOOM_AND_PAN.md`
- `PRE_PR_CLEANUP.md`
- `RELEASE_CHECKLIST.md`

## Status

The current editor supports static/single-frame FRM UI workflows. Animated/multi-frame FRM editing is not supported yet.
