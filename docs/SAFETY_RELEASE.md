# Safety / Release notes

This sprint adds small safety checks around the stable static UI workflow.

## Editor safety checks

- The editor refuses to overwrite the opened base image when exporting PNG/BMP.
- The editor refuses to overwrite the source FRM directly when exporting FRM.
- Text objects only require an AAF font when at least one text item exists.
- The **Check project** button summarizes loaded files, object counts, and common warnings.

## CLI safety checks

`frm-import` refuses to write the output over the original FRM path. Export to a new file first, then copy it into the game/mod folder after testing.

## Recommended release validation

Before publishing a build, test this workflow with a small static UI FRM:

1. Open ACT.
2. Open FRM.
3. Open AAF.
4. Add one erase patch.
5. Add one translated text.
6. Save a `.fui.json` project.
7. Export BMP 8-bit.
8. Export FRM with a new filename.
9. Run `frm-info` on the original and edited FRM.
10. Test the edited FRM in-game.
