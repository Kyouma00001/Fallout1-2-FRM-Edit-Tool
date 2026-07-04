# Visual editor FRM workflow

This sprint adds direct FRM support to the visual editor for static/single-frame Fallout FRM files.

## Buttons

- Open ACT: load the 256-color ACT palette used for preview/export.
- Open FRM: open a static/single-frame FRM as the editor base image.
- Export BMP 8-bit: export the current composition as indexed BMP.
- Export FRM: write a new FRM using the opened FRM as the template.

## Recommended workflow

1. Open ACT.
2. Open FRM.
3. Open AAF.
4. Add erase patches over old text.
5. Add translated text.
6. Save project.
7. Export FRM.

## Notes

- Only static/single-frame FRM files are supported.
- Export FRM preserves the original FRM structure and writes a new file.
- When a FRM is used as the base, the editor keeps the original indexed pixel data and applies erase/text changes on top of those indices during export.
- Do not overwrite the original game FRM directly. Export to a new file first and test it in-game.
