# Release Checklist

This project is not packaged as a user-facing release yet. Use this checklist before creating release artifacts.

## Source hygiene

```bash
git status --short
git ls-files | grep -Ei '\.(frm|aaf|act|pal|bmp|png|ttf)$'
git submodule foreach git status --short
```

Do not include game files, edited FRM files, ACT palettes, AAF fonts, BMP exports, PNG previews, or generated TTF files in release source commits.

## Build validation

```bash
dotnet clean
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet list package --vulnerable
```

## Manual editor validation

Test the full static FRM workflow:

1. Open ACT.
2. Open FRM.
3. Open AAF.
4. Add erase patch.
5. Add translated text.
6. Save project.
7. Reopen project.
8. Export BMP 8-bit.
9. Export edited FRM.
10. Test the edited FRM in-game.

## Source ZIP

For a clean source archive, prefer Git instead of zipping the working directory:

```bash
git archive --format=zip --output Fallout-1-2-Tools-source.zip HEAD
```

This avoids accidentally including `.git`, `bin/`, `obj/`, local exports, and ignored game assets.

## Future binary packaging

The next release sprint should create a Windows package with published CLI/UI binaries, for example:

```bash
dotnet publish src/Fallout.Tools.CLI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/win-x64/cli
dotnet publish src/Fallout.Tools.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/win-x64/ui
```

Then zip only the `dist/` output intended for users.
