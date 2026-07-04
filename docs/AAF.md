# Fallout AAF Font Format

This document describes the `.AAF` font format used by Fallout 1/2 fonts such as `FONT3.AAF` and `FONT4.AAF`.

Status: **MVP draft**. The reader/exporter is implemented from observed files and the reference editor behavior.

## Constants

| Name | Value |
|---|---:|
| Signature | `AAFF` |
| Header size | `0x000C` / 12 bytes |
| Glyph count | 256 |
| Glyph table entry size | 8 bytes |
| Bitmap data base offset | `0x080C` |

`0x080C = 12 + (256 * 8)`.

## Header

| Offset | Size | Endian | Meaning |
|---:|---:|---|---|
| `0x00` | 4 | ASCII | Signature: `AAFF` |
| `0x04` | 2 | Big-endian | Maximum/line height |
| `0x06` | 6 | raw | Unknown/reserved; preserve when saving |

Observed header tail in provided samples:

```text
00 01 00 04 00 04
```

## Glyph table

The glyph table starts at `0x000C` and contains 256 entries.

Each entry is 8 bytes:

| Relative offset | Size | Endian | Meaning |
|---:|---:|---|---|
| `+0` | 2 | Big-endian | Glyph width |
| `+2` | 2 | Big-endian | Glyph height |
| `+4` | 4 | Big-endian | Bitmap data offset relative to `0x080C` |

The glyph index normally corresponds to the byte/character code. Examples:

| Index | Character |
|---:|---|
| 32 | Space |
| 33 | `!` |
| 48 | `0` |
| 65 | `A` |
| 97 | `a` |

A space glyph may have width but zero height, e.g. `width=4`, `height=0`.

## Bitmap data

Bitmap data starts at absolute offset `0x080C`.

For each glyph:

```text
absoluteOffset = 0x080C + glyph.DataOffset
pixelCount = glyph.Width * glyph.Height
```

Each pixel is stored as one byte. Value `0` is transparent/empty. Non-zero values are brightness/color levels; the editor UI commonly treats them as values `1..9`.

Pixels are stored row-major:

```text
pixelIndex = y * width + x
```

AAF glyph images are bottom-aligned against the font max height when previewed/exported.

## Notes for future writer

When saving `.AAF`:

1. Preserve the original 12-byte header, unless changing line height intentionally.
2. Rebuild the glyph table.
3. Store each glyph's pixel data contiguously after `0x080C`.
4. Update each glyph data offset relative to `0x080C`.
