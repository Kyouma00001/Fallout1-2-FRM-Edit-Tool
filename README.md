# Font Match only cleanup

This package keeps the Sprint 11 `font-match` CLI command and removes the experimental Text Style Fidelity UI changes by restoring the visual editor MainWindow to the Sprint 10 version.

After extracting this package, delete these files if they exist:

```bash
rm -f src/Fallout.Tools.Core/Imaging/TextStyleProcessor.cs
rm -f docs/TEXT_STYLE_FIDELITY.md
```

Then run:

```bash
dotnet build
dotnet test
```

Commit only the font-match files and the reverted MainWindow:

```bash
git status --short
git add src/Fallout.Tools.CLI/Program.cs src/Fallout.Tools.Core/FontMatching/AafFontMatcher.cs src/Fallout.Tools.UI/MainWindow.cs docs/FONT_MATCH.md README.md
git add -u
git commit -m "feat(fonts): add AAF font matching"
git push
```
