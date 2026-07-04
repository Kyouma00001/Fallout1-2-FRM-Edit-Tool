# Render batch sprint

Adds `render-batch` to generate multiple UI text PNGs from one UTF-8 text file.

Example:

```bash
dotnet run --project src/Fallout.Tools.CLI -- render-batch samples/FONT4.AAF samples/render-batch.example.txt exports/ui-text --scale 1 --palette orange --uppercase
```

Input format:

```text
BARTER=negociar
TALK=falar
DONE=pronto
```
