# 8-bit BMP export with ACT palette

For final Fallout FRM conversion, use `Export BMP 8-bit` instead of PNG.

Recommended workflow:

1. Open the clean UI image.
2. Open the AAF font.
3. Open the ACT palette used by the FRM conversion tool.
4. Add and position translated text.
5. Export BMP 8-bit.

The editor writes a Windows BMP with:

- 8 bits per pixel
- 256-color palette
- palette loaded from Adobe `.act` files
- nearest-color mapping for pixels that are not already in the palette

PNG export remains available only as a preview/debug export.
