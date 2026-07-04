# Sprint 11 - Polish / Safety / Release

Apply this package after the stable FRM visual editor branch.

## Validate

```bash
dotnet build
dotnet test
dotnet run --project src/Fallout.Tools.UI
```

## Commit

```bash
git status --short
git add src/Fallout.Tools.CLI/Program.cs src/Fallout.Tools.UI/MainWindow.cs docs/SAFETY_RELEASE.md
git commit -m "chore(ui): add export safety checks"
git push
```
