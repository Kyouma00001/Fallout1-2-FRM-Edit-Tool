# Windows release / packaging

This document describes how to create a Windows ZIP package for Fallout 1/2 Tools.

## Requirements

- Windows 10/11
- .NET 8 SDK installed
- Clean working tree
- No game assets committed to Git

## Build package

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/publish-windows.ps1
```

The script will:

1. restore packages
2. build in Release mode
3. run tests in Release mode
4. publish the CLI as a single-file Windows executable
5. publish the UI as a single-file Windows executable
6. create a ZIP in `dist/`

Default output:

```text
dist/Fallout.Tools.win-x64.zip
```

Package layout:

```text
Fallout.Tools.win-x64/
├─ cli/
│  └─ FalloutFontTool.exe
├─ ui/
│  └─ Fallout.Tools.UI.exe
├─ docs/
│  └─ RELEASE_CHECKLIST.md
├─ README.txt
└─ PROJECT_README.md
```

## Framework-dependent build

A smaller package can be generated with:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/publish-windows.ps1 -FrameworkDependent
```

This requires users to have the matching .NET runtime installed. For public releases, prefer the default self-contained package.

## Manual validation before uploading a release

Run:

```bash
dotnet clean
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet list package --vulnerable
git status --short
git ls-files | grep -Ei '\.(frm|aaf|act|pal|bmp|png|ttf)$'
```

Then test the packaged UI from `dist/Fallout.Tools.win-x64/ui/`:

1. Open ACT.
2. Open static FRM.
3. Open AAF.
4. Add erase patch.
5. Add translated text.
6. Test zoom buttons, mouse wheel zoom, and canvas pan.
7. Save and reopen a project.
8. Export BMP 8-bit.
9. Export FRM to a new file.
10. Test the edited FRM in-game/mod.

## Release notes template

```markdown
## Fallout 1/2 Tools - v0.1.0

### Highlights

- Visual editor for static Fallout 1/2 UI FRM files
- AAF text rendering
- ACT palette support
- BMP 8-bit export
- Static FRM export
- Erase/clone patches
- Project save/load
- Recent file directories
- Zoom controls, mouse wheel zoom, and canvas panning

### Validation

- Built with .NET 8
- Release build passed
- Tests passed
- No known vulnerable NuGet packages
- No game assets included in repository or release package

### Notes

This is an early release focused on static/single-frame UI FRM workflows. Animated/multi-frame FRM files are not supported yet.
```
