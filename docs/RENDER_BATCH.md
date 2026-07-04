# Batch text rendering

`render-batch` renders multiple text PNGs from a single UTF-8 text file.

This is useful for UI translation work because you can keep a list of interface labels and generate all pixel-perfect PNGs at once.

## Usage

```bash
dotnet run --project src/Fallout.Tools.CLI -- render-batch samples/FONT4.AAF samples/render-batch.example.txt exports/ui-text --scale 1 --palette orange --uppercase
```

## Text file format

Each non-empty line must use:

```text
OUTPUT_NAME=Text to render
```

Example:

```text
BARTER=negociar
TALK=falar
DONE=pronto
INVENTORY=inventário
OPTIONS=opções
```

Lines beginning with `#` are ignored.

The output names are converted to PNG filenames. For example:

```text
BARTER=negociar
```

creates:

```text
BARTER.png
```

## Notes

Use `--uppercase` when the target UI uses uppercase text. The command preserves the original AAF glyphs; if uppercase accented glyphs look small, that is part of the original font data.
