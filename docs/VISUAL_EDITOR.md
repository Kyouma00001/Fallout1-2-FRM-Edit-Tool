# Visual UI Text Editor

The visual editor can place AAF-rendered text over a clean static UI image.

## Text sizing controls

- `Scale`: integer pixel scale. Use `1` for original AAF size.
- `Width scale`: stretches or compresses the rendered text horizontally. Examples: `0.85`, `1.0`, `1.2`.
- `Height scale`: stretches or compresses vertically. Keep this at `1.0` unless needed.
- `Letter spacing`: adds pixels between glyphs before width/height scaling.

For translations, prefer:

1. adjust position and alignment box;
2. adjust letter spacing;
3. adjust width scale;
4. adjust general scale.

Width/height scaling uses nearest-neighbor resizing to preserve pixel-art hard edges.
