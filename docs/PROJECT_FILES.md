# Fallout UI project files

The visual editor can now save and open a complete project file.

Use **Save project** to write a `.fui.json` file. Use **Open project** to restore it later.

The project file stores:

- base image path
- AAF font path
- ACT palette path
- text items
- text position, width, scale, width scale, height scale, letter spacing, alignment and uppercase flag
- erase / clone patches
- erase target position and size
- erase source position

Paths are saved relative to the project file when possible, so keeping the project file near the working images makes it easier to move the folder.

## Recommended workflow

1. Open the clean UI image.
2. Open the AAF font.
3. Open the ACT palette.
4. Add erase patches and translated text.
5. Save project as `my-screen.fui.json`.
6. Export BMP 8-bit when ready.

## Git note

Do not commit game assets such as `.FRM`, `.AAF`, `.PAL`, `.ACT`, extracted `.BMP`, or test exports unless you are sure they are legally redistributable.

Project files may contain local paths, so review them before committing.
