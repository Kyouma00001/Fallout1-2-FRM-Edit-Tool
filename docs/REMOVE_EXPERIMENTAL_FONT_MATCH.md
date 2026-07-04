# Remove experimental font match / text style features

This cleanup restores the CLI and visual editor to the stable FRM workflow state before Sprint 11 experiments.

It removes/avoids:

- font-match command
- AafFontMatcher.cs
- FONT_MATCH.md
- TextStyleProcessor.cs
- TEXT_STYLE_FIDELITY.md

After extracting this package, remove any experimental files that may still exist:

```bash
rm -rf src/Fallout.Tools.Core/FontMatching
rm -f src/Fallout.Tools.Core/Imaging/TextStyleProcessor.cs
rm -f docs/FONT_MATCH.md
rm -f docs/TEXT_STYLE_FIDELITY.md
```

Then validate:

```bash
dotnet build
dotnet test
dotnet run --project src/Fallout.Tools.UI
```
