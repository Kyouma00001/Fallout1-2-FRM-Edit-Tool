# Render uppercase hotfix

Adds `--uppercase` / `--force-uppercase` to the `render` command.

Example:

```bash
dotnet run --project src/Fallout.Tools.CLI -- render samples/FONT4.AAF "negociação" exports/NEGOCIACAO.png --scale 1 --palette orange --uppercase
```

This keeps the original AAF glyphs, but converts the input text to uppercase before rendering.
