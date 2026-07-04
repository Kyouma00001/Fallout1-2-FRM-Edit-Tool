# UI compose workflow

`compose-ui` overlays text rendered from a Fallout `.AAF` font onto a base UI image.

This is intended for translation work where a `.FRM` frame was converted to `.BMP` or `.PNG`, cleaned manually, and now needs localized text placed back with the original Fallout font pixels.

## Command

```bash
dotnet run --project src/Fallout.Tools.CLI -- compose-ui <font.aaf> <base.png|base.bmp> <layout.txt> <output.png> [options]
```

Example:

```bash
dotnet run --project src/Fallout.Tools.CLI -- compose-ui samples/FONT4.AAF samples/ui-compose-base.png samples/ui-compose.example.txt exports/ui-composed.png --scale 1 --palette orange --uppercase
```

## Layout format

Simple placement:

```text
NAME|X|Y|TEXT
```

Aligned placement inside a fixed-width region:

```text
NAME|X|Y|WIDTH|ALIGN|TEXT
```

`ALIGN` can be:

- `left`
- `center`
- `right`

Example:

```text
BARTER|10|8|70|center|negociar
TALK|90|8|50|center|falar
DONE|150|8|60|center|pronto
```

## Important notes

`compose-ui` does not remove the original English text from the base image. For now, the base image should already have the old text erased or covered.

This command saves a PNG. If you need to return to `.FRM`, convert/reimport the composed image using your existing FRM workflow while preserving the Fallout palette.
