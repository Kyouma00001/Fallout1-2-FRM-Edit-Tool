# Erase / Clone Patches

The visual editor can now create erase patches before text rendering.

An erase patch copies pixels from a clean source rectangle of the base image and pastes them over a target rectangle. This is useful for covering original baked-in UI text before rendering translated AAF text.

## Workflow

1. Open the clean/base UI image.
2. Open the AAF font.
3. Open the ACT palette for 8-bit BMP export.
4. Click **Add erase** or **Add erase patch**.
5. Move the orange target rectangle over the old text.
6. Adjust **Source X** and **Source Y** so the green source rectangle points to a clean texture area.
7. Resize the patch using the small handle.
8. Add translated text above the erased area.
9. Export with **Export BMP 8-bit** for FRM conversion.

## Layout format

Erase patches are saved before text entries:

```text
ERASE|NAME|X|Y|WIDTH|HEIGHT|SOURCE_X|SOURCE_Y
```

Example:

```text
ERASE|ERASE_DONE|150|8|60|18|150|32
```

Text lines keep the existing format.
