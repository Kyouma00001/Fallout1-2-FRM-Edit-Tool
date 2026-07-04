# Pre-PR Cleanup

Use this checklist before opening a pull request from the polish/release branch.

## 1. Validate build and tests

```bash
dotnet clean
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet list package --vulnerable
```

Expected result:

- build succeeds
- tests pass
- no vulnerable packages are listed

## 2. Check Git state

```bash
git status --short
git log --oneline --decorate main..HEAD
git diff main...HEAD --stat
```

The working tree should be clean before opening the PR.

## 3. Check for accidentally tracked assets

```bash
git ls-files | grep -Ei '\.(frm|aaf|act|pal|bmp|png|ttf)$'
```

This should normally return nothing. If something appears, verify whether it is a safe synthetic fixture. Do not commit original Fallout files or derived UI exports.

## 4. Check submodule state

```bash
git submodule status --recursive
git submodule foreach git status --short
```

If the reference submodule is dirty and you do not intend to update it, reset it:

```bash
git submodule update --init --recursive
git restore reference/Fallout2FontEditor
```

## 5. Manual UI validation

Run the editor:

```bash
dotnet run --project src/Fallout.Tools.UI
```

Test:

1. Open ACT.
2. Open FRM.
3. Open AAF.
4. Add erase patch.
5. Move target patch and source patch.
6. Add translated text.
7. Move/resize text.
8. Save project.
9. Close and reopen editor.
10. Open project.
11. Use zoom buttons.
12. Use mouse wheel zoom.
13. Pan canvas by dragging the background.
14. Export BMP 8-bit.
15. Export FRM to a new filename.
16. Confirm source FRM overwrite is refused.
17. Test the edited FRM in-game.

## 6. Suggested PR title

```text
Polish visual editor safety, theme, zoom, and export workflow
```

## 7. Suggested PR description

```markdown
## Summary

Improves the Fallout UI visual editor before release.

## Changes

- Adds export safety checks for FRM/BMP/PNG workflows
- Prevents overwriting source FRM files
- Updates ImageSharp security patch version
- Adds Fallout-inspired editor theme
- Reorganizes toolbar and side panel sections
- Adds project status indicators for FRM/base, AAF, and ACT
- Remembers recent directories for file pickers
- Adds visual zoom controls
- Adds mouse wheel zoom
- Adds canvas panning with mouse drag
- Updates README and cleanup documentation

## Validation

- dotnet build -c Release
- dotnet test -c Release
- dotnet list package --vulnerable
- Manual UI test with ACT, FRM, AAF, erase patch, text, BMP export, and FRM export
```
